# MQTT Pattern

> All patterns and API reference: [Patterns Index](https://raw.githubusercontent.com/contensive/Contensive5/refs/heads/master/patterns/index.md)

## Overview

The MQTT pattern enables Contensive addons to communicate with remote IoT devices through AWS IoT Core. Addons can publish JSON commands to devices and subscribe to receive JSON messages from devices. The system uses X.509 certificate authentication over MQTT with TLS (port 8883) and the MQTTnet client library.

## Architecture

```
IoT Device (ESP32)  <-->  AWS IoT Core  <-->  Contensive TaskService
                                                    |
                                               MQTTSubscriptionService
                                               (persistent MQTTnet connection)
                                                    |
                                               On message received:
                                               -> match topic to subscriptions
                                               -> queue handler addons via TaskRunner
```

**Outbound (commands to devices):** An addon calls `cp.MQTT.Publish(topic, json)`. The system opens a short-lived TLS connection to AWS IoT Core, publishes the message with QoS AtLeastOnce, and disconnects.

**Inbound (messages from devices):** The `MQTTSubscriptionService` runs inside TaskService as a long-lived MQTT connection. It subscribes to all active topic filters stored in the `ccMqttSubscriptions` database table. When a message arrives, it matches the topic against registered filters (including `+` and `#` wildcards), looks up the handler addon for each match, and queues it as a task. The TaskRunner then executes the addon with `mqtt-topic` and `mqtt-message` available as doc properties.

### Key Components

- **CPMQTTBaseClass** — abstract API exposed as `cp.MQTT` to addon developers
- **MQTTController** — handles MQTT publishing using MQTTnet with X.509 TLS
- **MQTTSubscriptionService** — long-lived subscriber in TaskService, dispatches incoming messages to handler addons
- **ccMqttSubscriptions** — database table that maps topic filters to handler addons

---

## Administrator Setup

### 1. AWS IoT Core Configuration

Create an IoT "Thing" and certificates in the AWS IoT Core console:

1. In AWS IoT Core, create a Thing for your server (e.g. `contensive-server`)
2. Create and download a certificate for the Thing:
   - Device certificate (`.pem.crt`)
   - Private key (`.pem.key`)
   - Amazon root CA certificate (`AmazonRootCA1.pem`)
3. Convert the device certificate and private key into a PFX file:
   ```
   openssl pkcs12 -export -in certificate.pem.crt -inkey private.pem.key -out device.pfx -password pass:YourPassword
   ```
4. Create an IoT Policy that allows the server to publish and subscribe:
   ```json
   {
     "Version": "2012-10-17",
     "Statement": [
       {
         "Effect": "Allow",
         "Action": ["iot:Connect", "iot:Publish", "iot:Subscribe", "iot:Receive"],
         "Resource": "*"
       }
     ]
   }
   ```
5. Attach the policy to the certificate, and the certificate to the Thing

### 2. Certificate Files

Upload the following files to the Contensive **privateFiles** folder (e.g. in a `mqtt/` subfolder):

- The PFX certificate file (e.g. `mqtt/device.pfx`)
- The Amazon root CA certificate (e.g. `mqtt/AmazonRootCA1.pem`)

### 3. Site Properties

Set these site properties in the Contensive admin under Settings > Site Properties:

| Site Property | Value | Example |
|---|---|---|
| `mqtt endpoint` | Your AWS IoT Core endpoint | `a1b2c3d4e5f6g7-ats.iot.us-east-1.amazonaws.com` |
| `mqtt broker port` | MQTT TLS port (default 8883) | `8883` |
| `mqtt certificate filename` | Path to PFX file in privateFiles | `mqtt/device.pfx` |
| `mqtt certificate password` | Password used when creating the PFX | `YourPassword` |
| `mqtt root certificate filename` | Path to root CA cert in privateFiles | `mqtt/AmazonRootCA1.pem` |

You can find your endpoint in the AWS IoT Core console under **Settings > Device data endpoint**.

### 4. TaskService

The `MQTTSubscriptionService` starts automatically when the Contensive TaskService starts. It checks every 30 seconds for:
- MQTT connection health (reconnects if disconnected)
- New or removed subscriptions in `ccMqttSubscriptions`

No additional TaskService configuration is required beyond ensuring the service is running. The service only activates for apps that have the `mqtt endpoint` site property set.

---

## Addon Developer Guide

### Publishing Commands to a Device

Use `cp.MQTT.Publish(topic, messageJson)` to send a JSON command to a device:

```csharp
public class SetTemperatureAddon : AddonBaseClass {
    public override object Execute(CPBaseClass cp) {
        try {
            string deviceId = cp.Doc.GetText("deviceId");
            int temperature = cp.Doc.GetInteger("temperature");
            //
            var command = new {
                action = "set-temp",
                value = temperature,
                timestamp = DateTime.UtcNow.ToString("o")
            };
            string json = System.Text.Json.JsonSerializer.Serialize(command);
            //
            bool success = cp.MQTT.Publish($"cmd/{deviceId}/set-temp", json);
            return success ? "Command sent" : "Failed to send command";
        } catch (Exception ex) {
            cp.Site.ErrorReport(ex);
            return "There was an error sending the command.";
        }
    }
}
```

### Subscribing to Device Messages

Use `cp.MQTT.Subscribe(topicFilter, addonId)` to register a handler addon. This is typically done once, for example in an OnInstall event catcher addon or a setup addon:

```csharp
public class RegisterMqttSubscriptions : AddonBaseClass {
    public override object Execute(CPBaseClass cp) {
        try {
            //
            // -- look up handler addons by guid to get their ids
            int thermostatHandlerId = cp.Addon.GetIdByGuid("{YOUR-HANDLER-ADDON-GUID}");
            int alertHandlerId = cp.Addon.GetIdByGuid("{YOUR-ALERT-ADDON-GUID}");
            int statusHandlerId = cp.Addon.GetIdByGuid("{YOUR-STATUS-ADDON-GUID}");
            //
            // -- subscribe to all events from all thermostats
            cp.MQTT.Subscribe("evt/thermostat/#", thermostatHandlerId);
            //
            // -- subscribe to alerts from a specific device
            cp.MQTT.Subscribe("evt/pump-001/alert", alertHandlerId);
            //
            // -- subscribe to status from any device type using single-level wildcard
            cp.MQTT.Subscribe("evt/+/status", statusHandlerId);
            //
            return "";
        } catch (Exception ex) {
            cp.Site.ErrorReport(ex);
            return "There was an error registering MQTT subscriptions.";
        }
    }
}
```

**MQTT Wildcards:**
- `+` matches exactly one topic level (e.g. `evt/+/status` matches `evt/thermostat-001/status` but not `evt/thermostat-001/temp/status`)
- `#` matches zero or more topic levels and must be the last character (e.g. `evt/thermostat/#` matches `evt/thermostat/status`, `evt/thermostat/001/temp`, etc.)

### Handling Incoming Messages

The handler addon receives the topic and message as doc properties:

```csharp
public class ThermostatEventHandler : AddonBaseClass {
    public override object Execute(CPBaseClass cp) {
        try {
            string topic = cp.Doc.GetText("mqtt-topic");
            string messageJson = cp.Doc.GetText("mqtt-message");
            //
            // -- parse the device ID from the topic
            // -- e.g. topic = "evt/thermostat-001/temp" -> parts[1] = "thermostat-001"
            string[] topicParts = topic.Split('/');
            string deviceId = topicParts.Length > 1 ? topicParts[1] : "unknown";
            //
            // -- deserialize the JSON payload
            var reading = System.Text.Json.JsonSerializer.Deserialize<DeviceReading>(messageJson);
            //
            // -- store the reading in the database
            using (var cs = cp.CSNew()) {
                cs.Insert("Device Readings");
                cs.SetField("deviceId", deviceId);
                cs.SetField("temperature", reading.temperature);
                cs.SetField("humidity", reading.humidity);
                cs.SetField("readingDate", DateTime.UtcNow);
                cs.Save();
            }
            //
            return "";
        } catch (Exception ex) {
            cp.Site.ErrorReport(ex);
            return "There was an error processing the device event.";
        }
    }
}

public class DeviceReading {
    public double temperature { get; set; }
    public double humidity { get; set; }
}
```

### Unsubscribing

To stop receiving messages for a topic filter:

```csharp
cp.MQTT.Unsubscribe("evt/thermostat/#", thermostatHandlerId);
```

This deactivates the subscription record in `ccMqttSubscriptions`. The MQTTSubscriptionService will unsubscribe from the MQTT broker on its next 30-second tick.

### Collection Registration

Register the handler addon and the subscription setup addon in your collection XML:

```xml
<!-- The handler addon that processes incoming MQTT messages -->
<Addon Name="Thermostat Event Handler" Guid="{YOUR-HANDLER-ADDON-GUID}" Type="Add-on">
    <DotNetClass>MyNamespace.ThermostatEventHandler</DotNetClass>
</Addon>

<!-- The setup addon that registers MQTT subscriptions (runs on install) -->
<Addon Name="Register MQTT Subscriptions" Guid="{YOUR-SETUP-ADDON-GUID}" Type="Add-on">
    <DotNetClass>MyNamespace.RegisterMqttSubscriptions</DotNetClass>
</Addon>

<!-- Register the setup addon as an event catcher for "Addon Collection Installed" -->
<Data>
    <Record Content="Add-on Events" Guid="{YOUR-EVENT-GUID}" Name="Addon Collection Installed" />
    <Record Content="Add-on Event Catchers" Guid="{YOUR-CATCHER-GUID}">
        <field Name="addonId">{YOUR-SETUP-ADDON-GUID}</field>
        <field Name="eventId">{YOUR-EVENT-GUID}</field>
    </Record>
</Data>
```

---

## IoT Device Developer Guide

This section describes how to set up an ESP32 or similar IoT device to communicate with Contensive through AWS IoT Core.

### 1. AWS IoT Core Device Setup

1. In the AWS IoT Core console, create a Thing for your device (e.g. `thermostat-001`)
2. Create and download a certificate for the device
3. Create an IoT Policy for the device that allows it to publish and subscribe to its topics:
   ```json
   {
     "Version": "2012-10-17",
     "Statement": [
       {
         "Effect": "Allow",
         "Action": ["iot:Connect"],
         "Resource": "arn:aws:iot:us-east-1:ACCOUNT:client/thermostat-001"
       },
       {
         "Effect": "Allow",
         "Action": ["iot:Publish"],
         "Resource": "arn:aws:iot:us-east-1:ACCOUNT:topic/evt/thermostat-001/*"
       },
       {
         "Effect": "Allow",
         "Action": ["iot:Subscribe"],
         "Resource": "arn:aws:iot:us-east-1:ACCOUNT:topicfilter/cmd/thermostat-001/*"
       },
       {
         "Effect": "Allow",
         "Action": ["iot:Receive"],
         "Resource": "arn:aws:iot:us-east-1:ACCOUNT:topic/cmd/thermostat-001/*"
       }
     ]
   }
   ```
4. Attach the policy to the certificate, and the certificate to the Thing

### 2. Topic Convention

Use this topic structure for communication between Contensive and IoT devices:

| Direction | Topic Pattern | Example |
|---|---|---|
| Server to device (commands) | `cmd/{device-id}/{action}` | `cmd/thermostat-001/set-temp` |
| Device to server (events) | `evt/{device-id}/{event-type}` | `evt/thermostat-001/temp` |
| Device to server (alerts) | `evt/{device-id}/alert` | `evt/thermostat-001/alert` |
| Device to server (status) | `evt/{device-id}/status` | `evt/thermostat-001/status` |

### 3. ESP32 Arduino Example

This example uses the Arduino framework with the `WiFiClientSecure` and `PubSubClient` libraries:

```cpp
#include <WiFiClientSecure.h>
#include <PubSubClient.h>
#include <ArduinoJson.h>

// -- AWS IoT Core endpoint (same as in Contensive site properties)
const char* mqttEndpoint = "a1b2c3d4e5f6g7-ats.iot.us-east-1.amazonaws.com";
const int mqttPort = 8883;
const char* deviceId = "thermostat-001";

// -- device certificates (stored in flash or SPIFFS)
const char* deviceCert = "-----BEGIN CERTIFICATE-----\n...\n-----END CERTIFICATE-----";
const char* privateKey = "-----BEGIN RSA PRIVATE KEY-----\n...\n-----END RSA PRIVATE KEY-----";
const char* rootCA = "-----BEGIN CERTIFICATE-----\n...\n-----END CERTIFICATE-----";

WiFiClientSecure wifiClient;
PubSubClient mqttClient(wifiClient);

// -- called when a command message is received from Contensive
void messageCallback(char* topic, byte* payload, unsigned int length) {
    // -- parse the JSON command
    StaticJsonDocument<256> doc;
    DeserializationError err = deserializeJson(doc, payload, length);
    if (err) { return; }

    const char* action = doc["action"];
    if (strcmp(action, "set-temp") == 0) {
        int targetTemp = doc["value"];
        // -- apply the temperature setting to your hardware
        setThermostatTarget(targetTemp);
    }
}

void connectMQTT() {
    wifiClient.setCACert(rootCA);
    wifiClient.setCertificate(deviceCert);
    wifiClient.setPrivateKey(privateKey);

    mqttClient.setServer(mqttEndpoint, mqttPort);
    mqttClient.setCallback(messageCallback);

    while (!mqttClient.connected()) {
        if (mqttClient.connect(deviceId)) {
            // -- subscribe to commands from Contensive
            mqttClient.subscribe("cmd/thermostat-001/#");
        } else {
            delay(5000);
        }
    }
}

void publishReading(float temperature, float humidity) {
    StaticJsonDocument<128> doc;
    doc["temperature"] = temperature;
    doc["humidity"] = humidity;

    char buffer[128];
    serializeJson(doc, buffer);

    // -- publish to the event topic that Contensive is subscribed to
    mqttClient.publish("evt/thermostat-001/temp", buffer);
}

void setup() {
    // -- connect WiFi, then MQTT
    connectWiFi();
    connectMQTT();
}

void loop() {
    if (!mqttClient.connected()) { connectMQTT(); }
    mqttClient.loop();

    // -- publish a reading every 60 seconds
    static unsigned long lastPublish = 0;
    if (millis() - lastPublish > 60000) {
        float temp = readTemperature();
        float hum = readHumidity();
        publishReading(temp, hum);
        lastPublish = millis();
    }
}
```

### 4. JSON Message Format

All messages between Contensive and IoT devices should be JSON. Keep payloads under **128 KB** (AWS IoT Core limit).

**Device to server (event):**
```json
{
    "temperature": 72.5,
    "humidity": 45.2
}
```

**Server to device (command):**
```json
{
    "action": "set-temp",
    "value": 68,
    "timestamp": "2026-04-22T14:30:00Z"
}
```

**Device alert:**
```json
{
    "alert": "high-temp",
    "value": 98.6,
    "threshold": 90.0,
    "message": "Temperature exceeded threshold"
}
```

### 5. AWS IoT Core Limits

| Limit | Value |
|---|---|
| Max message payload | 128 KB |
| Max publishes per second per connection | 100 |
| Max throughput per connection | 512 KB/s |
| Max topic levels (forward slashes) | 7 |

---

## Best Practices

1. **Use specific topic filters** when subscribing. Avoid `#` (match-all) in production — subscribe only to the topics your addon needs.
2. **Keep JSON payloads small.** AWS IoT Core charges per 5 KB increment. A 7 KB message counts as 2 billable messages.
3. **Include a timestamp** in command messages so devices can detect stale commands.
4. **Use one handler addon per message type.** A thermostat reading handler and an alert handler should be separate addons with separate topic subscriptions.
5. **Restrict IoT policies per device.** Each device certificate should only allow publishing/subscribing to that device's topics, not `*`.
6. **Handle reconnection gracefully.** The MQTTSubscriptionService auto-reconnects every 30 seconds. IoT devices should also implement reconnection with backoff.
7. **Test with the AWS IoT Core MQTT test client.** The AWS console includes an MQTT test client where you can publish and subscribe to topics to debug message flow.
