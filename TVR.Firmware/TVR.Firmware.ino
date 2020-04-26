#define CONTROLLER_RED
// #define CONTROLLER_BLUE

#include <ESP8266WiFi.h>

#include "Constants.h"
#include "WiFiConfig.h"
#include "Discovery.h"
#include "MPUSensor.h"
#include "Button.h"
#include "Packet.h"

Button trigger(TRIGGER_PIN);
Discovery discovery;

IPAddress serverIp;
WiFiUDP udp;

MPUSensor mpu;

void setup() {
  Serial.begin(38400);    // Ah yes, debug
  Serial.println("Twometer VR Firmware v2.0");

  pinMode(TRIGGER_PIN, INPUT_PULLUP); // Configure our pin

  WiFi.persistent(true);  // Save those credentials
  WiFi.mode(WIFI_STA);    // Station mode for ESP-firmware glitch prevention
  WiFi.begin(WIFI_SSID, WIFI_PASS); // Connect

  Serial.println("Connecting to WiFi...");
  while (WiFi.status() != WL_CONNECTED) {
    delay(500);
  }
  Serial.println("Connected.");

  Serial.println("Discovering server...");
  serverIp = discovery.discover();
  Serial.print("Discovered server at ");
  Serial.println(serverIp);
  if (!udp.begin(CONTROLLER_PORT))
    Serial.println("Can't initialize UDP client");

  Serial.println("Initializing MPU...");
  bool ok = mpu.begin();
  if (ok)
    Serial.println("Initialized");
  else {
    Serial.println("Initialization failed");
    while (true) delay(500); // Loop forever on failure
  }
  mpu.beginCalibration();
}

void loop() {
  if (mpu.hasData()) {
    mpu.update();

    if (mpu.isCalibrated())
      sendPackets();
  }
}

void sendPackets() {
  if (trigger.isPressed()) {
    byte buttons[] = { BUTTON_A };
    Packet::Send(&udp, serverIp, 1, buttons, mpu.getYaw(), mpu.getPitch(), mpu.getRoll());
  } else {
    Packet::Send(&udp, serverIp, 0, NULL, mpu.getYaw(), mpu.getPitch(), mpu.getRoll());
  }
}
