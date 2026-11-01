# ESP32 Setup Instructions

This guide walks you through setting up the ESP32 device to communicate with Contensive via MQTT.

## Prerequisites

- Computer with USB port (Windows, Mac, or Linux)
- ESP32 hardware assembled per [hardware-wiring.md](hardware-wiring.md)
- Active WiFi network
- Contensive server with MQTT addon installed

## Step 1: Install Arduino IDE

1. Download Arduino IDE from https://www.arduino.cc/en/software
2. Install version 2.x or later (recommended) or 1.8.19+
3. Launch Arduino IDE

## Step 2: Install ESP32 Board Support

1. Open **File → Preferences** (or **Arduino IDE → Settings** on Mac)
2. In "Additional Board Manager URLs" field, add:
   ```
   https://raw.githubusercontent.com/espressif/arduino-esp32/gh-pages/package_esp32_index.json
   ```
3. Click **OK**
4. Open **Tools → Board → Boards Manager**
5. Search for "esp32"
6. Install "esp32 by Espressif Systems" (version 2.0.0 or later)
7. Click **Close**

## Step 3: Install Required Libraries

### 3.1 PubSubClient (MQTT Library)

1. Open **Tools → Manage Libraries** (or **Sketch → Include Library → Manage Libraries**)
2. Search for "PubSubClient"
3. Install "PubSubClient by Nick O'Leary" (version 2.8.0 or later)

### 3.2 DHT Sensor Library

1. In the Library Manager, search for "DHT sensor library"
2. Install "DHT sensor library by Adafruit" (version 1.4.0 or later)
3. When prompted, also install dependencies:
   - "Adafruit Unified Sensor"

### 3.3 ArduinoJson

1. In the Library Manager, search for "ArduinoJson"
2. Install "ArduinoJson by Benoit Blanchon" (version 6.x or later)

## Step 4: Configure the Arduino Sketch

1. Open [contensive-mqtt-test.ino](contensive-mqtt-test.ino) in Arduino IDE
2. Locate the CONFIGURATION section at the top of the file
3. Update the following values:

### WiFi Settings
```cpp
const char* WIFI_SSID = "YourWiFiSSID";      // Your WiFi network name
const char* WIFI_PASSWORD = "YourWiFiPassword"; // Your WiFi password
```

### MQTT Broker Settings
```cpp
const char* MQTT_BROKER = "192.168.1.100";   // IP address of Contensive server
const int MQTT_PORT = 1883;                   // MQTT port (default: 1883)
const char* MQTT_USER = "contensive";         // MQTT username (if required)
const char* MQTT_PASSWORD = "password";       // MQTT password (if required)
```

### Device ID (Optional)
```cpp
const char* DEVICE_ID = "{53BDF919-DA65-4F0A-A5F2-53624C3C3E17}";
```

To generate a new device ID, run in PowerShell:
```powershell
powershell -Command "[guid]::NewGuid().ToString('B').ToUpper()"
```

### Pin Configuration (if needed)
```cpp
#define DHT_PIN 4          // DHT22 data pin
#define BUTTON_PIN 5       // Push button pin
#define LED_PIN 16         // LED pin (use 2 for built-in LED)
```

## Step 5: Select Board and Port

1. Connect ESP32 to computer via USB cable
2. In Arduino IDE, select **Tools → Board → esp32 → ESP32 Dev Module**
   - Or select your specific ESP32 board model if different
3. Select **Tools → Port** → Select the COM port (Windows) or /dev/ttyUSB* (Linux/Mac)
   - On Windows: Usually COM3, COM4, etc.
   - On Mac: Usually /dev/cu.usbserial-*
   - On Linux: Usually /dev/ttyUSB0

### Board Configuration (Advanced)
If needed, adjust these settings under **Tools** menu:
- **Upload Speed**: 115200
- **CPU Frequency**: 240MHz (default)
- **Flash Frequency**: 80MHz (default)
- **Flash Mode**: QIO (default)
- **Flash Size**: 4MB (default)
- **Partition Scheme**: Default
- **Core Debug Level**: None (use "Verbose" for troubleshooting)

## Step 6: Compile and Upload

1. Click the **Verify** button (checkmark icon) to compile
   - Fix any errors that appear in the console
2. Click the **Upload** button (right arrow icon)
   - ESP32 should enter programming mode automatically
   - If not, hold the "BOOT" button on ESP32 while clicking Upload
3. Wait for upload to complete (usually 20-30 seconds)
4. You should see "Done uploading" message

## Step 7: Monitor Serial Output

1. Open **Tools → Serial Monitor**
2. Set baud rate to **115200** in the dropdown
3. You should see output similar to:

```
=================================
Contensive MQTT Test Device
=================================
Connecting to WiFi: YourWiFiSSID
.....
WiFi connected!
IP address: 192.168.1.150
Connecting to MQTT broker: 192.168.1.100
MQTT connected!
Subscribed to command topics
Status published: online
Setup complete!
```

## Step 8: Verify Functionality

### Test 1: Button Press
1. Press the physical button on the breadboard
2. Serial monitor should show: "Button pressed!"
3. LED should toggle on/off
4. MQTT message published to `contensive/devices/{deviceId}/telemetry/button`

### Test 2: Temperature Reading
1. Wait 30 seconds (default telemetry interval)
2. Serial monitor should show temperature and humidity readings
3. MQTT messages published to temperature and humidity topics

### Test 3: LED Control (requires Contensive)
1. From Contensive, publish MQTT command to control LED
2. Topic: `contensive/devices/{deviceId}/commands/led`
3. Payload: `{"action":"on"}` or `{"action":"off"}`
4. LED should respond to command

## Troubleshooting

### Upload Failed
- **Error**: "Failed to connect to ESP32"
- **Solution**:
  - Check USB cable (try a different one)
  - Try holding BOOT button during upload
  - Check COM port selection
  - Install USB-to-Serial drivers if needed (CP210x or CH340)

### WiFi Connection Failed
- **Error**: "WiFi connection failed!"
- **Solution**:
  - Verify SSID and password are correct
  - Ensure WiFi network is 2.4GHz (ESP32 doesn't support 5GHz)
  - Check WiFi signal strength

### MQTT Connection Failed
- **Error**: "MQTT connection failed, rc=-2" (or other codes)
- **Solution**:
  - Verify MQTT broker IP address
  - Check MQTT broker is running (test with MQTT Explorer or similar tool)
  - Verify port 1883 is open
  - Check username/password if authentication is enabled
  - Common return codes:
    - -4: Timeout
    - -2: Network connection failed
    - 5: Not authorized (check username/password)

### DHT Sensor Not Reading
- **Error**: "Failed to read from DHT sensor!"
- **Solution**:
  - Verify wiring (VCC, GND, DATA pins)
  - Wait 2 seconds after power-on for sensor to stabilize
  - Try adding 10kΩ pull-up resistor between DATA and VCC
  - Ensure you're using DHT22 (not DHT11)

### LED Not Working
- **Solution**:
  - Check LED polarity
  - Verify current-limiting resistor is in place
  - Test with built-in LED (GPIO 2) first
  - Check GPIO pin number matches wiring

### Serial Monitor Shows Garbage Characters
- **Solution**:
  - Set baud rate to 115200
  - Click reset button on ESP32
  - Check serial port selection

## Next Steps

Once the device is working, proceed to [testing-guide.md](testing-guide.md) to integrate with Contensive MQTT addon.

## Additional Resources

- ESP32 Documentation: https://docs.espressif.com/projects/esp-idf/en/latest/esp32/
- PubSubClient Library: https://pubsubclient.knolleary.net/
- DHT Library: https://github.com/adafruit/DHT-sensor-library
- MQTT Protocol: https://mqtt.org/
