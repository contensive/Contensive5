# Testing Guide - ESP32 with Contensive MQTT

This guide walks through testing the ESP32 device integration with Contensive CMS via MQTT.

## Prerequisites

- ESP32 device programmed and running (see [setup-instructions.md](setup-instructions.md))
- Contensive server with MQTT addon installed
- MQTT broker running (Mosquitto or built-in to Contensive)
- MQTT client tool for testing (MQTT Explorer, mosquitto_sub/pub, or MQTT.fx)

## Test Environment Setup

### Option 1: Using MQTT Explorer (Recommended)

1. Download MQTT Explorer from http://mqtt-explorer.com/
2. Install and launch
3. Configure connection:
   - **Name**: Contensive Test
   - **Host**: Your Contensive server IP
   - **Port**: 1883
   - **Username/Password**: If required
4. Click **Connect**

### Option 2: Using Mosquitto Command-Line Tools

Install Mosquitto clients:
```bash
# Windows (via Chocolatey)
choco install mosquitto

# Mac (via Homebrew)
brew install mosquitto

# Linux (Ubuntu/Debian)
sudo apt-get install mosquitto-clients
```

## Test 1: Verify Device Connection

### Expected Behavior
When the ESP32 powers on and connects, it should:
1. Connect to WiFi
2. Connect to MQTT broker
3. Subscribe to command topics
4. Publish "online" status

### Verification Steps

**Using MQTT Explorer:**
1. Navigate to topic tree: `contensive/devices/{deviceId}/status`
2. You should see the message: `online`
3. Check timestamp - should be recent

**Using Mosquitto:**
```bash
mosquitto_sub -h 192.168.1.100 -t "contensive/devices/+/status" -v
```
Expected output:
```
contensive/devices/{53BDF919-DA65-4F0A-A5F2-53624C3C3E17}/status online
```

## Test 2: Button Press Event

### Expected Behavior
When you press the button on the ESP32:
1. LED should toggle on/off
2. Serial monitor shows "Button pressed!"
3. MQTT message published with event data

### Verification Steps

**Using MQTT Explorer:**
1. Subscribe to: `contensive/devices/{deviceId}/telemetry/button`
2. Press the button on ESP32
3. You should see JSON message appear:
```json
{
  "deviceId": "{53BDF919-DA65-4F0A-A5F2-53624C3C3E17}",
  "deviceName": "ESP32-Test-Device-01",
  "timestamp": 123456,
  "event": "pressed"
}
```

**Using Mosquitto:**
```bash
mosquitto_sub -h 192.168.1.100 -t "contensive/devices/+/telemetry/button" -v
```

Press button and verify message appears.

## Test 3: Temperature and Humidity Telemetry

### Expected Behavior
Every 30 seconds (configurable), the device should:
1. Read DHT22 sensor
2. Publish temperature reading
3. Publish humidity reading
4. Display readings in serial monitor

### Verification Steps

**Using MQTT Explorer:**
1. Subscribe to: `contensive/devices/{deviceId}/telemetry/#`
   - The `#` wildcard captures all telemetry subtopics
2. Wait up to 30 seconds
3. You should see two messages:

**Temperature:**
```json
{
  "deviceId": "{53BDF919-DA65-4F0A-A5F2-53624C3C3E17}",
  "deviceName": "ESP32-Test-Device-01",
  "timestamp": 123456,
  "temperature": 22.5,
  "unit": "celsius"
}
```

**Humidity:**
```json
{
  "deviceId": "{53BDF919-DA65-4F0A-A5F2-53624C3C3E17}",
  "deviceName": "ESP32-Test-Device-01",
  "timestamp": 123456,
  "humidity": 45.2,
  "unit": "percent"
}
```

**Using Mosquitto:**
```bash
# Subscribe to all telemetry
mosquitto_sub -h 192.168.1.100 -t "contensive/devices/+/telemetry/#" -v
```

## Test 4: LED Control Commands

### Expected Behavior
When Contensive (or you, via MQTT) publishes a command:
1. ESP32 receives the command
2. LED responds accordingly
3. Device publishes new LED state
4. Serial monitor shows confirmation

### Verification Steps

**Test 4.1: Turn LED On**

Using MQTT Explorer:
1. Navigate to topic: `contensive/devices/{deviceId}/commands/led`
2. Click "Publish" button
3. Enter payload:
```json
{"action":"on"}
```
4. LED should turn on
5. Serial monitor shows: "LED turned ON"

Using Mosquitto:
```bash
mosquitto_pub -h 192.168.1.100 \
  -t "contensive/devices/{53BDF919-DA65-4F0A-A5F2-53624C3C3E17}/commands/led" \
  -m '{"action":"on"}'
```

**Test 4.2: Turn LED Off**

Payload:
```json
{"action":"off"}
```

**Test 4.3: Toggle LED**

Payload:
```json
{"action":"toggle"}
```

### Verify LED State Publishing

After each command, device publishes state to:
`contensive/devices/{deviceId}/state/led`

Expected message:
```json
{
  "deviceId": "{53BDF919-DA65-4F0A-A5F2-53624C3C3E17}",
  "deviceName": "ESP32-Test-Device-01",
  "timestamp": 123456,
  "state": "on"
}
```

## Test 5: Contensive Integration

### Setup Contensive MQTT Addon

Based on [mqtt-plan-implementation.md](../mqtt-plan-implementation.md):

1. Install Contensive MQTT addon
2. Configure MQTT broker connection
3. Register the ESP32 device in Contensive
4. Set up automation rules (optional)

### Test 5.1: Device Registration

1. In Contensive admin, navigate to MQTT Devices
2. Add new device with Device ID: `{53BDF919-DA65-4F0A-A5F2-53624C3C3E17}`
3. Set device name: "ESP32 Test Device 01"
4. Save
5. Verify device shows as "Online" in Contensive

### Test 5.2: View Telemetry in Contensive

1. Navigate to device detail page
2. View telemetry history
3. Verify temperature, humidity, and button events appear
4. Check timestamps are correct

### Test 5.3: Control LED from Contensive

1. On device control panel, click "Turn LED On"
2. Verify LED on ESP32 lights up
3. Click "Turn LED Off"
4. Verify LED turns off
5. Check device state updates in real-time

### Test 5.4: Automation Rules (if implemented)

Example rule: "Turn on LED when temperature exceeds 25°C"

1. Create automation rule in Contensive
2. Heat the DHT22 sensor (use your hand or warm air)
3. Wait for telemetry cycle (up to 30 seconds)
4. Verify LED automatically turns on when threshold crossed
5. Cool sensor down
6. Verify LED automatically turns off

## Test 6: Offline/Online Behavior

### Test Last Will and Testament (LWT)

1. Subscribe to status topic
2. Unplug ESP32 (simulate power loss)
3. After a few seconds, MQTT broker should publish LWT message:
   - Topic: `contensive/devices/{deviceId}/status`
   - Payload: `offline`
4. Verify Contensive marks device as "Offline"
5. Plug ESP32 back in
6. Verify device reconnects and publishes `online` status

## Test 7: Multiple Devices

To test multiple devices:

1. Build a second ESP32 device
2. Generate a new device GUID:
   ```powershell
   powershell -Command "[guid]::NewGuid().ToString('B').ToUpper()"
   ```
3. Update the `DEVICE_ID` in the sketch
4. Upload to second ESP32
5. Verify both devices show in Contensive
6. Test controlling each device independently

## Performance Testing

### Message Frequency
- Default: Telemetry every 30 seconds
- To change interval, modify `TELEMETRY_INTERVAL` in sketch:
  ```cpp
  #define TELEMETRY_INTERVAL 10000  // 10 seconds
  ```

### Stress Test
1. Rapidly press button multiple times
2. Verify all messages are received
3. Check for message loss or delays
4. Monitor ESP32 memory usage in serial monitor

## Troubleshooting Common Issues

### Device Shows Offline in Contensive
- Check serial monitor for MQTT connection errors
- Verify network connectivity
- Check MQTT broker is running
- Verify device ID matches in both ESP32 and Contensive

### Commands Not Reaching Device
- Verify topic names match exactly (case-sensitive)
- Check device is subscribed (look in serial monitor on startup)
- Test with MQTT Explorer to confirm broker receives messages
- Check JSON format is valid

### Telemetry Not Appearing in Contensive
- Verify topic subscription in Contensive addon
- Check JSON parsing in Contensive (format must match expected schema)
- Test with MQTT Explorer to confirm messages are published
- Check timestamps are valid

### Sensor Readings Show NaN or -999
- Check DHT22 wiring
- Verify 3.3V power
- Allow 2 seconds for sensor initialization after power-on
- Try adding pull-up resistor if needed

## MQTT Topic Reference

All topics for device `{53BDF919-DA65-4F0A-A5F2-53624C3C3E17}`:

### Published by Device (to Contensive)
```
contensive/devices/{53BDF919-DA65-4F0A-A5F2-53624C3C3E17}/status
contensive/devices/{53BDF919-DA65-4F0A-A5F2-53624C3C3E17}/telemetry/button
contensive/devices/{53BDF919-DA65-4F0A-A5F2-53624C3C3E17}/telemetry/temperature
contensive/devices/{53BDF919-DA65-4F0A-A5F2-53624C3C3E17}/telemetry/humidity
contensive/devices/{53BDF919-DA65-4F0A-A5F2-53624C3C3E17}/state/led
```

### Subscribed by Device (from Contensive)
```
contensive/devices/{53BDF919-DA65-4F0A-A5F2-53624C3C3E17}/commands/led
contensive/devices/{53BDF919-DA65-4F0A-A5F2-53624C3C3E17}/commands/config
```

## Success Criteria

All tests pass when:
- ✓ Device connects to WiFi and MQTT broker
- ✓ Status messages (online/offline) publish correctly
- ✓ Button press events are captured and published
- ✓ Temperature/humidity telemetry publishes every 30 seconds
- ✓ LED responds to remote commands
- ✓ Device shows correctly in Contensive admin
- ✓ Telemetry data appears in Contensive
- ✓ Automation rules trigger based on sensor data
- ✓ Device gracefully handles disconnect/reconnect

## Next Steps

After successful testing:
1. Deploy additional ESP32 devices
2. Create custom automation rules in Contensive
3. Build dashboard views for device monitoring
4. Implement additional sensor types
5. Add OTA (Over-The-Air) firmware updates
6. Implement device configuration via MQTT
