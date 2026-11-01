/*
 * Contensive MQTT Test Device - ESP32
 *
 * This sketch demonstrates IoT device integration with Contensive CMS
 * via MQTT protocol. The device publishes sensor data and receives commands.
 *
 * Hardware:
 * - ESP32 DevKit board
 * - DHT22 temperature/humidity sensor on GPIO 4
 * - Push button on GPIO 5 (with pull-down resistor)
 * - LED on GPIO 16 (with current-limiting resistor)
 *
 * MQTT Topics:
 * Published:
 *   - contensive/devices/{deviceId}/status
 *   - contensive/devices/{deviceId}/telemetry/button
 *   - contensive/devices/{deviceId}/telemetry/temperature
 *   - contensive/devices/{deviceId}/telemetry/humidity
 *
 * Subscribed:
 *   - contensive/devices/{deviceId}/commands/led
 *   - contensive/devices/{deviceId}/commands/config
 */

#include <WiFi.h>
#include <PubSubClient.h>
#include <DHT.h>
#include <ArduinoJson.h>

// ============================================================================
// CONFIGURATION - MODIFY THESE VALUES
// ============================================================================

// WiFi credentials
const char* WIFI_SSID = "YourWiFiSSID";
const char* WIFI_PASSWORD = "YourWiFiPassword";

// MQTT broker settings
const char* MQTT_BROKER = "192.168.1.100";  // Your Contensive server IP
const int MQTT_PORT = 1883;
const char* MQTT_USER = "contensive";        // Optional: MQTT username
const char* MQTT_PASSWORD = "password";      // Optional: MQTT password

// Device identification
const char* DEVICE_ID = "{53BDF919-DA65-4F0A-A5F2-53624C3C3E17}";
const char* DEVICE_NAME = "ESP32-Test-Device-01";

// Pin definitions
#define DHT_PIN 4          // DHT22 data pin
#define BUTTON_PIN 5       // Push button pin
#define LED_PIN 16         // LED pin (use 2 for built-in LED)

// Sensor settings
#define DHT_TYPE DHT22
#define TELEMETRY_INTERVAL 30000  // Send telemetry every 30 seconds
#define BUTTON_DEBOUNCE 50        // Button debounce delay in ms

// ============================================================================
// GLOBAL OBJECTS AND VARIABLES
// ============================================================================

WiFiClient wifiClient;
PubSubClient mqttClient(wifiClient);
DHT dht(DHT_PIN, DHT_TYPE);

// MQTT topic strings
char topicStatus[128];
char topicTelemetryButton[128];
char topicTelemetryTemp[128];
char topicTelemetryHumidity[128];
char topicCommandLed[128];
char topicCommandConfig[128];

// State variables
unsigned long lastTelemetryTime = 0;
unsigned long lastButtonTime = 0;
int lastButtonState = LOW;
bool ledState = false;

// ============================================================================
// SETUP
// ============================================================================

void setup() {
  Serial.begin(115200);
  delay(1000);

  Serial.println("\n\n=================================");
  Serial.println("Contensive MQTT Test Device");
  Serial.println("=================================");

  // Initialize hardware
  pinMode(BUTTON_PIN, INPUT);
  pinMode(LED_PIN, OUTPUT);
  digitalWrite(LED_PIN, LOW);

  // Initialize DHT sensor
  dht.begin();

  // Build MQTT topic strings
  buildTopicStrings();

  // Connect to WiFi
  connectWiFi();

  // Configure MQTT
  mqttClient.setServer(MQTT_BROKER, MQTT_PORT);
  mqttClient.setCallback(mqttCallback);
  mqttClient.setBufferSize(512);  // Increase buffer for JSON messages

  // Connect to MQTT broker
  connectMQTT();

  Serial.println("Setup complete!");
}

// ============================================================================
// MAIN LOOP
// ============================================================================

void loop() {
  // Maintain MQTT connection
  if (!mqttClient.connected()) {
    connectMQTT();
  }
  mqttClient.loop();

  // Check button state
  handleButton();

  // Send periodic telemetry
  unsigned long currentTime = millis();
  if (currentTime - lastTelemetryTime >= TELEMETRY_INTERVAL) {
    lastTelemetryTime = currentTime;
    sendTelemetry();
  }
}

// ============================================================================
// WIFI CONNECTION
// ============================================================================

void connectWiFi() {
  Serial.print("Connecting to WiFi: ");
  Serial.println(WIFI_SSID);

  WiFi.begin(WIFI_SSID, WIFI_PASSWORD);

  int attempts = 0;
  while (WiFi.status() != WL_CONNECTED && attempts < 30) {
    delay(500);
    Serial.print(".");
    attempts++;
  }

  if (WiFi.status() == WL_CONNECTED) {
    Serial.println("\nWiFi connected!");
    Serial.print("IP address: ");
    Serial.println(WiFi.localIP());
  } else {
    Serial.println("\nWiFi connection failed!");
    Serial.println("Check SSID and password, then reset the device.");
    while (true) {
      delay(1000);
    }
  }
}

// ============================================================================
// MQTT CONNECTION
// ============================================================================

void connectMQTT() {
  while (!mqttClient.connected()) {
    Serial.print("Connecting to MQTT broker: ");
    Serial.println(MQTT_BROKER);

    // Create a unique client ID
    String clientId = String("ESP32-") + String(DEVICE_ID);

    // Attempt to connect
    bool connected;
    if (strlen(MQTT_USER) > 0) {
      connected = mqttClient.connect(clientId.c_str(), MQTT_USER, MQTT_PASSWORD,
                                     topicStatus, 1, true, "offline");
    } else {
      connected = mqttClient.connect(clientId.c_str(), topicStatus, 1, true, "offline");
    }

    if (connected) {
      Serial.println("MQTT connected!");

      // Subscribe to command topics
      mqttClient.subscribe(topicCommandLed);
      mqttClient.subscribe(topicCommandConfig);
      Serial.println("Subscribed to command topics");

      // Publish online status
      publishStatus("online");

    } else {
      Serial.print("MQTT connection failed, rc=");
      Serial.println(mqttClient.state());
      Serial.println("Retrying in 5 seconds...");
      delay(5000);
    }
  }
}

// ============================================================================
// MQTT CALLBACK - HANDLE INCOMING MESSAGES
// ============================================================================

void mqttCallback(char* topic, byte* payload, unsigned int length) {
  Serial.print("Message received on topic: ");
  Serial.println(topic);

  // Convert payload to string
  String message = "";
  for (unsigned int i = 0; i < length; i++) {
    message += (char)payload[i];
  }
  Serial.print("Payload: ");
  Serial.println(message);

  // Handle LED command
  if (strcmp(topic, topicCommandLed) == 0) {
    handleLedCommand(message);
  }

  // Handle config command
  if (strcmp(topic, topicCommandConfig) == 0) {
    handleConfigCommand(message);
  }
}

// ============================================================================
// COMMAND HANDLERS
// ============================================================================

void handleLedCommand(String message) {
  // Parse JSON command
  StaticJsonDocument<256> doc;
  DeserializationError error = deserializeJson(doc, message);

  if (error) {
    Serial.print("JSON parse error: ");
    Serial.println(error.c_str());
    return;
  }

  // Extract command
  const char* action = doc["action"];

  if (action) {
    if (strcmp(action, "on") == 0) {
      digitalWrite(LED_PIN, HIGH);
      ledState = true;
      Serial.println("LED turned ON");
    } else if (strcmp(action, "off") == 0) {
      digitalWrite(LED_PIN, LOW);
      ledState = false;
      Serial.println("LED turned OFF");
    } else if (strcmp(action, "toggle") == 0) {
      ledState = !ledState;
      digitalWrite(LED_PIN, ledState ? HIGH : LOW);
      Serial.println(ledState ? "LED toggled ON" : "LED toggled OFF");
    }

    // Send confirmation
    publishLedState();
  }
}

void handleConfigCommand(String message) {
  Serial.println("Config command received (not implemented in this demo)");
  // In a production device, this would update telemetry intervals,
  // sensor thresholds, etc.
}

// ============================================================================
// BUTTON HANDLING
// ============================================================================

void handleButton() {
  int buttonState = digitalRead(BUTTON_PIN);

  // Detect button press (LOW to HIGH transition)
  if (buttonState == HIGH && lastButtonState == LOW) {
    unsigned long currentTime = millis();

    // Debounce check
    if (currentTime - lastButtonTime > BUTTON_DEBOUNCE) {
      lastButtonTime = currentTime;
      Serial.println("Button pressed!");

      // Publish button event
      publishButtonPress();

      // Toggle LED as visual feedback
      ledState = !ledState;
      digitalWrite(LED_PIN, ledState ? HIGH : LOW);
      publishLedState();
    }
  }

  lastButtonState = buttonState;
}

// ============================================================================
// TELEMETRY PUBLISHING
// ============================================================================

void sendTelemetry() {
  Serial.println("Sending telemetry...");

  // Read sensor data
  float humidity = dht.readHumidity();
  float temperature = dht.readTemperature();  // Celsius

  // Check if reading failed
  if (isnan(humidity) || isnan(temperature)) {
    Serial.println("Failed to read from DHT sensor!");
    return;
  }

  Serial.print("Temperature: ");
  Serial.print(temperature);
  Serial.print("°C, Humidity: ");
  Serial.print(humidity);
  Serial.println("%");

  // Publish temperature
  publishTemperature(temperature);

  // Publish humidity
  publishHumidity(humidity);
}

void publishButtonPress() {
  StaticJsonDocument<256> doc;
  doc["deviceId"] = DEVICE_ID;
  doc["deviceName"] = DEVICE_NAME;
  doc["timestamp"] = millis();
  doc["event"] = "pressed";

  char jsonBuffer[256];
  serializeJson(doc, jsonBuffer);

  mqttClient.publish(topicTelemetryButton, jsonBuffer, false);
  Serial.println("Button press published");
}

void publishTemperature(float temperature) {
  StaticJsonDocument<256> doc;
  doc["deviceId"] = DEVICE_ID;
  doc["deviceName"] = DEVICE_NAME;
  doc["timestamp"] = millis();
  doc["temperature"] = temperature;
  doc["unit"] = "celsius";

  char jsonBuffer[256];
  serializeJson(doc, jsonBuffer);

  mqttClient.publish(topicTelemetryTemp, jsonBuffer, false);
}

void publishHumidity(float humidity) {
  StaticJsonDocument<256> doc;
  doc["deviceId"] = DEVICE_ID;
  doc["deviceName"] = DEVICE_NAME;
  doc["timestamp"] = millis();
  doc["humidity"] = humidity;
  doc["unit"] = "percent";

  char jsonBuffer[256];
  serializeJson(doc, jsonBuffer);

  mqttClient.publish(topicTelemetryHumidity, jsonBuffer, false);
}

void publishLedState() {
  StaticJsonDocument<256> doc;
  doc["deviceId"] = DEVICE_ID;
  doc["deviceName"] = DEVICE_NAME;
  doc["timestamp"] = millis();
  doc["state"] = ledState ? "on" : "off";

  char jsonBuffer[256];
  serializeJson(doc, jsonBuffer);

  // Publish to a state topic (optional, for monitoring)
  char topicLedState[128];
  snprintf(topicLedState, sizeof(topicLedState),
           "contensive/devices/%s/state/led", DEVICE_ID);

  mqttClient.publish(topicLedState, jsonBuffer, true);  // Retained message
}

void publishStatus(const char* status) {
  mqttClient.publish(topicStatus, status, true);  // Retained message
  Serial.print("Status published: ");
  Serial.println(status);
}

// ============================================================================
// TOPIC STRING BUILDERS
// ============================================================================

void buildTopicStrings() {
  snprintf(topicStatus, sizeof(topicStatus),
           "contensive/devices/%s/status", DEVICE_ID);

  snprintf(topicTelemetryButton, sizeof(topicTelemetryButton),
           "contensive/devices/%s/telemetry/button", DEVICE_ID);

  snprintf(topicTelemetryTemp, sizeof(topicTelemetryTemp),
           "contensive/devices/%s/telemetry/temperature", DEVICE_ID);

  snprintf(topicTelemetryHumidity, sizeof(topicTelemetryHumidity),
           "contensive/devices/%s/telemetry/humidity", DEVICE_ID);

  snprintf(topicCommandLed, sizeof(topicCommandLed),
           "contensive/devices/%s/commands/led", DEVICE_ID);

  snprintf(topicCommandConfig, sizeof(topicCommandConfig),
           "contensive/devices/%s/commands/config", DEVICE_ID);
}
