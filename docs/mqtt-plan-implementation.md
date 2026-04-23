# cp.MQTT Interface for AWS IoT Core — Plan & Implementation

## Context

The existing `cp.MQTT` interface had only a `Publish()` method (throwing `NotImplementedException`) and referenced the abandoned M2Mqtt library. This implementation provides a full-featured MQTT interface that lets Contensive addon developers:
- **Send** JSON commands to ESP32/IoT devices via AWS IoT Core
- **Receive** JSON messages from devices, with their addon called automatically

AWS IoT Core uses X.509 certificate authentication over native MQTT (port 8883). We use **MQTTnet 4.3.7** as the client library and maintain a **long-lived subscription in TaskService** that dispatches incoming messages to handler addons via the task queue.

---

## API Design: `cp.MQTT`

### CPMQTTBaseClass (abstract, in CPBase)

```csharp
public abstract class CPMQTTBaseClass {
    // -- Publish a JSON message to a device topic
    public abstract bool Publish(string topic, string messageJson);

    // -- Subscribe an addon to receive messages on a topic pattern
    //    addonId: the id of the addon to execute when a message arrives
    //    topicFilter: MQTT topic filter (supports +/# wildcards)
    public abstract void Subscribe(string topicFilter, int addonId);

    // -- Unsubscribe an addon from a topic pattern
    public abstract void Unsubscribe(string topicFilter, int addonId);

    // -- Legacy overload (deprecated)
    [Obsolete] public abstract bool Publish(string message, string topic, string clientId);
}
```

### How addon developers use it

**Publishing (sending a command to a device):**
```csharp
public override object Execute(CPBaseClass cp) {
    var command = new { action = "set-temp", value = 72 };
    cp.MQTT.Publish("cmd/thermostat-001", JsonSerializer.Serialize(command));
    return "";
}
```

**Subscribing (receiving messages from devices):**
```csharp
// Subscribe is called once at startup (e.g. in an OnInstall event catcher addon)
public override object Execute(CPBaseClass cp) {
    cp.MQTT.Subscribe("evt/thermostat-001/#", myHandlerAddonId);
    return "";
}
```

**Handler addon (called when message arrives):**
```csharp
public class ThermostatMessageHandler : AddonBaseClass {
    public override object Execute(CPBaseClass cp) {
        string topic = cp.Doc.GetText("mqtt-topic");
        string message = cp.Doc.GetText("mqtt-message");
        // Process the incoming JSON message
        var data = JsonSerializer.Deserialize<ThermostatEvent>(message);
        // ... handle it
        return "";
    }
}
```

---

## Architecture

```
ESP32 Device  <-->  AWS IoT Core  <-->  Contensive TaskService
                                             |
                                        MQTTSubscriptionService
                                        (long-lived MQTTnet connection)
                                             |
                                        On message received:
                                        -> lookup subscriber addons in ccMqttSubscriptions
                                        -> execute each via TaskRunner
                                        (topic + message passed as doc properties)
```

---

## Implementation Details

### 1. MQTTnet NuGet package

- **File:** `source/Processor/Processor.csproj`
- Added `<PackageReference Include="MQTTnet" Version="4.3.7.1207" />`

### 2. Database table: ccMqttSubscriptions

- **File:** `source/Processor/aoBase51.xml` (collection XML — added first per project rules)
- CDef `MQTT Subscriptions` with table `ccMqttSubscriptions`:
  - `Name` (text, inherited from base content definition)
  - `topicFilter` (text, required) — the MQTT topic filter pattern
  - `addonId` (lookup into Add-ons, required) — the addon to call when a message matches
  - `active` (boolean, managed by Subscribe/Unsubscribe)
- Guid: `{A1F3B4C5-D6E7-48F9-A0B1-C2D3E4F5A6B7}`

### 3. CPMQTTBaseClass (abstract API)

- **File:** `source/CPBase/BaseClasses/CPMqttBaseClass.cs`
- New methods:
  - `Publish(string topic, string messageJson)` — simplified, clientId managed internally
  - `Subscribe(string topicFilter, int addonId)` — register an addon to receive messages
  - `Unsubscribe(string topicFilter, int addonId)` — deactivate a subscription
- Legacy `Publish(message, topic, clientId)` marked `[Obsolete]`

### 4. MQTTController (publish implementation)

- **File:** `source/Processor/Controllers/MQTTController.cs`
- Replaced commented-out M2Mqtt code with MQTTnet implementation
- Certificate-based TLS auth using existing site properties
- `buildClientOptions(clientId)` — constructs `MqttClientOptions` with X.509 certs and TLS 1.2
- `publish(topic, messageJson)` — creates a short-lived connection, publishes with QoS AtLeastOnce, disconnects
- Existing site property names preserved (endpoint, port, cert files)
- Certificate validation uses `X509Chain` with the root CA cert in `ExtraStore`

### 5. MQTTSubscriptionService (subscription handler)

- **New file:** `source/Processor/Controllers/MQTTSubscriptionService.cs`
- Implements `IDisposable`, follows same timer pattern as `TaskSchedulerController`
- 30-second timer tick:
  - Iterates all enabled apps in `serverConfig`
  - Ensures MQTT client is connected (auto-reconnects if disconnected)
  - Loads active subscriptions from `ccMqttSubscriptions`
  - Subscribes to new topic filters, unsubscribes from removed ones
- On message received:
  - Matches incoming topic against all active subscription filters using MQTT wildcard rules (`+` and `#`)
  - For each matching subscription, looks up the addon by id
  - Queues the addon as a task via `TaskSchedulerController.addTaskToQueue()` with `mqtt-topic` and `mqtt-message` as args
- `topicMatchesFilter()` — internal static method implementing MQTT topic matching with `+` (single-level) and `#` (multi-level) wildcards

### 6. CPMQTTClass (concrete implementation)

- **File:** `source/Processor/Views/CPMQTTClass.cs`
- `Publish(topic, messageJson)` → delegates to `MQTTController.publish()`
- `Subscribe(topicFilter, addonId)` → checks for existing record in `ccMqttSubscriptions`, reactivates or inserts
- `Unsubscribe(topicFilter, addonId)` → sets `active=0` on matching record

### 7. TaskService integration

- **File:** `source/TaskService/TaskService.cs`
- `OnStart()` — creates and starts `MQTTSubscriptionService` alongside existing `TaskSchedulerController` and `TaskRunnerController`
- `OnStop()` — stops and disposes the MQTT subscription service

---

## Site Properties (pre-existing, kept as-is)

| Property | Description | Default |
|---|---|---|
| `mqtt endpoint` | AWS IoT Core endpoint (e.g. `abc123-ats.iot.us-east-1.amazonaws.com`) | — |
| `mqtt broker port` | MQTT TLS port | `8883` |
| `mqtt certificate filename` | PFX certificate path in privateFiles | — |
| `mqtt certificate password` | PFX certificate password | — |
| `mqtt root certificate filename` | AWS root CA cert path in privateFiles | — |

---

## Message Flow

1. **Outbound (Publish):** Addon calls `cp.MQTT.Publish("cmd/device-001", json)` -> CPMQTTClass -> MQTTController -> MQTTnet client -> AWS IoT Core -> ESP32 device
2. **Inbound (Subscribe):** ESP32 device publishes -> AWS IoT Core -> MQTTSubscriptionService receives -> looks up matching addons in `ccMqttSubscriptions` -> queues task for each -> TaskRunner executes addon with `mqtt-topic` and `mqtt-message` as doc properties

---

## Files Modified/Created

| File | Action |
|---|---|
| `source/Processor/Processor.csproj` | Added MQTTnet 4.3.7.1207 NuGet reference |
| `source/Processor/aoBase51.xml` | Added `ccMqttSubscriptions` CDef with topicFilter and addonId (lookup) fields |
| `source/CPBase/BaseClasses/CPMqttBaseClass.cs` | Expanded abstract API: Publish, Subscribe, Unsubscribe |
| `source/Processor/Controllers/MQTTController.cs` | Rewrote with MQTTnet, X.509 TLS, builder pattern |
| `source/Processor/Controllers/MQTTSubscriptionService.cs` | **New** — long-lived MQTT subscriber with topic matching and task dispatch |
| `source/Processor/Views/CPMQTTClass.cs` | Implemented Subscribe/Unsubscribe via ccMqttSubscriptions table |
| `source/TaskService/TaskService.cs` | Start/stop MQTTSubscriptionService alongside existing services |

---

## Build Status

Solution builds successfully with 0 errors across both target frameworks (net48 and net9.0-windows).

---

## Verification Plan

1. **Unit-level:** MQTTController can connect to AWS IoT Core endpoint and publish a test message
2. **Subscribe flow:** Insert a test subscription record, send a message from AWS IoT test console, verify the handler addon executes and receives topic + message via `cp.Doc.GetText("mqtt-topic")` and `cp.Doc.GetText("mqtt-message")`
3. **Reconnect:** Kill the MQTT connection, verify the 30-second timer reconnects and resubscribes
4. **Multiple subscribers:** Two addons subscribe to overlapping topic filters, verify both execute
5. **Wildcard matching:** Test `+` (single-level) and `#` (multi-level) wildcards match correctly
