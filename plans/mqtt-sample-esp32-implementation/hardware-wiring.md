# ESP32 Hardware Wiring Guide

## Pin Assignments

| Component | ESP32 Pin | Notes |
|-----------|-----------|-------|
| DHT22 Data | GPIO 4 | Temperature/humidity sensor |
| Button | GPIO 5 | Pull-down with 10kΩ resistor |
| LED | GPIO 2 | Built-in LED on most ESP32 boards |
| External LED (optional) | GPIO 16 | With 220Ω current-limiting resistor |

## Component Details

### ESP32 DevKit Board
- 3.3V output pin for powering sensors
- GND pins for common ground
- GPIO pins for I/O
- USB port for programming and power

### DHT22 Temperature/Humidity Sensor
- VCC: Connect to 3.3V
- GND: Connect to GND
- DATA: Connect to GPIO 4

### Push Button
- One side: Connect to GPIO 5
- Other side: Connect to GND
- Pull-down resistor: 10kΩ between GPIO 5 and GND

### LED
- Anode (+): Connect through 220Ω resistor to GPIO 16
- Cathode (-): Connect to GND

## Breadboard Wiring Diagram

```
ESP32 DevKit
┌─────────────────────┐
│                     │
│  ┌───────────────┐  │
│  │   ESP32       │  │
│  │   Chip        │  │
│  └───────────────┘  │
│                     │
│ 3.3V ●             │───────┐
│                     │       │ (Power rail)
│  GND ●             │───────┼───────┐
│                     │       │       │ (Ground rail)
│  D4  ●             │───┐   │       │
│                     │   │   │       │
│  D5  ●             │─┐ │   │       │
│                     │ │ │   │       │
│  D16 ●             │─┼─┼─┐ │       │
│                     │ │ │ │ │       │
│  D2  ●  (built-in) │ │ │ │ │       │
│                     │ │ │ │ │       │
└─────────────────────┘ │ │ │ │       │
                        │ │ │ │       │
    DHT22               │ │ │ │       │
    ┌─────┐             │ │ │ │       │
    │  1  │ VCC ────────┘ │ │ │       │
    │  2  │ DATA ─────────┘ │ │       │
    │  3  │ NC               │ │       │
    │  4  │ GND ─────────────┼─┼───────┤
    └─────┘                  │ │       │
                             │ │       │
    Push Button              │ │       │
    ┌─┐                      │ │       │
    │ │──────────────────────┘ │       │
    │ │                        │       │
    │ │────────────────────────┼───────┤
    └─┘                        │       │
      │                        │       │
    [10kΩ]                     │       │
      │                        │       │
      └────────────────────────┼───────┤
                               │       │
    External LED               │       │
      ┌─────┐                  │       │
    ──┤ 220Ω├── Anode          │       │
      └─────┘      │            │       │
               ┌───▼───┐        │       │
               │  LED  │        │       │
               └───┬───┘        │       │
                   │ Cathode    │       │
                   └────────────┼───────┘
                                │
    ═══════════════════════════╧═══════  GND Rail

    ═══════════════════════════════════  3.3V Rail
```

## Detailed Connection Instructions

### Step 1: Power Rails
1. Connect ESP32 **3.3V** pin to breadboard's positive (+) power rail
2. Connect ESP32 **GND** pin to breadboard's negative (-) ground rail

### Step 2: DHT22 Sensor
1. Insert DHT22 into breadboard
2. Connect DHT22 pin 1 (VCC) to **3.3V** rail
3. Connect DHT22 pin 2 (DATA) to ESP32 **GPIO 4**
4. Connect DHT22 pin 4 (GND) to **GND** rail
   - Pin 3 (NC) is not connected

### Step 3: Push Button
1. Insert push button into breadboard (across center gap)
2. Connect one button pin to ESP32 **GPIO 5**
3. Connect same button pin through **10kΩ resistor** to **GND** rail
4. Connect other button pin directly to **GND** rail

### Step 4: LED
1. Insert LED into breadboard (mind polarity!)
   - Longer leg = Anode (+)
   - Shorter leg = Cathode (-)
2. Connect LED anode through **220Ω resistor** to ESP32 **GPIO 16**
3. Connect LED cathode to **GND** rail

### Step 5: USB Connection
1. Connect ESP32 to computer via USB cable
   - Provides power and programming interface

## Alternative: Using Built-in LED Only

Most ESP32 DevKit boards have a built-in LED on GPIO 2. If you want to simplify the build:

1. Skip the external LED and 220Ω resistor
2. In the Arduino sketch, change `LED_PIN` from 16 to 2
3. Use the built-in LED for testing

## Troubleshooting

### DHT22 Not Reading
- Verify 3.3V and GND connections
- Check data pin connection to GPIO 4
- Some DHT22 modules need a 10kΩ pull-up resistor between DATA and VCC

### Button Not Working
- Ensure pull-down resistor is connected
- Check GPIO 5 connection
- Verify button orientation in breadboard

### LED Not Lighting
- Check LED polarity (anode to resistor, cathode to GND)
- Verify 220Ω resistor is in series
- Test LED with 3.3V directly to verify it works

## Safety Notes

- ESP32 GPIO pins are **3.3V tolerant** - do not apply 5V
- Always use current-limiting resistor with LEDs
- DHT22 can run on 3.3V or 5V - we use 3.3V for simplicity
- Never connect power rails directly without load
