# ESP32 MQTT Test Implementation for Contensive

This folder contains a complete step-by-step guide to build an ESP32 device that interfaces with the Contensive MQTT implementation.

## What This Demonstrates

This ESP32 device implements a simple IoT node that:
- Publishes sensor data (button press, temperature) to Contensive
- Subscribes to commands from Contensive to control an LED
- Demonstrates the full MQTT communication pattern outlined in [mqtt-plan-implementation.md](../mqtt-plan-implementation.md)

## Hardware Required

- ESP32 development board (ESP32 DevKit or NodeMCU-32S) - ~$7
- DHT22 temperature/humidity sensor - ~$5
- Push button - ~$0.50
- LED (any color) - ~$0.10
- 10kΩ resistor (for button pull-down) - ~$0.05
- 220Ω resistor (for LED current limiting) - ~$0.05
- Breadboard and jumper wires - ~$5
- USB cable for programming

**Total cost: ~$18**

## Files in This Folder

1. [hardware-wiring.md](hardware-wiring.md) - Complete wiring diagram and pin connections
2. [arduino-sketch.ino](contensive-mqtt-test.ino) - Full Arduino code for ESP32
3. [setup-instructions.md](setup-instructions.md) - Step-by-step setup guide
4. [testing-guide.md](testing-guide.md) - How to test with Contensive

## Quick Start

1. Wire the hardware following [hardware-wiring.md](hardware-wiring.md)
2. Install Arduino IDE and ESP32 board support
3. Install required libraries (PubSubClient, DHT)
4. Configure WiFi and MQTT broker settings in the sketch
5. Upload the sketch to ESP32
6. Configure Contensive MQTT addon
7. Test the connection

See [setup-instructions.md](setup-instructions.md) for detailed steps.

## MQTT Topics Used

This device follows the Contensive MQTT topic pattern:

**Published by Device (to Contensive):**
- `contensive/devices/{deviceId}/status` - Device online/offline status
- `contensive/devices/{deviceId}/telemetry/button` - Button press events
- `contensive/devices/{deviceId}/telemetry/temperature` - Temperature readings
- `contensive/devices/{deviceId}/telemetry/humidity` - Humidity readings

**Subscribed by Device (from Contensive):**
- `contensive/devices/{deviceId}/commands/led` - LED control commands (on/off)
- `contensive/devices/{deviceId}/commands/config` - Configuration updates

## Device ID

Each ESP32 device has a unique ID. The sample code uses:
`{53BDF919-DA65-4F0A-A5F2-53624C3C3E17}`

You should generate a new GUID for each physical device you deploy.
