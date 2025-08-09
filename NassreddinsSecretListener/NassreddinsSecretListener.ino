/*
================================================================================
  Project: Nasreddin's Secret Listener (ESP32 / M5Stack Atom Lite + HMC5883L)
  Modules: BLE (NimBLE), LED Display (FastLED), Button Manager (DeepSleep),
           MagnetDetector (B² with dual-EMA, trend + look-ahead, auto-recalib)
  File:    NassreddinsSecretListener.ino   (main wiring)
================================================================================
*/

#include <Wire.h>
#include <Adafruit_Sensor.h>
#include <Adafruit_HMC5883_U.h>

#include "Config.h"
#include "LedDisplay.h"
#include "BleService.h"
#include "ButtonManager.h"
#include "MagnetDetector.h"

// --- Modules ---
Adafruit_HMC5883_Unified gMag(12345);
LedDisplay    gLed;
BleService    gBle;
ButtonManager gBtn;
MagnetDetector gDetector(gMag);

// --- Runtime ---
uint32_t gLastMs = 0;
MagnetState gLastSent = MagnetState::None;

void setup() {
  Serial.begin(115200);
  delay(100);

  gLed.begin();
  gBtn.begin();
  gBle.begin(BLE_DEVICE_NAME);

  if (!gDetector.begin()) {
    Serial.println("Magnetometer not found!");
    gLed.flashBlue(3, 80, 80);
    while (1) delay(200);
  }

  gLed.showState(MagnetState::None);
  gBle.notify(MagnetState::None);
  gLastMs = millis();
}

void loop() {
  uint32_t now = millis();
  uint32_t dt  = now - gLastMs; if (dt==0) dt=1; gLastMs = now;

  // Button: 7s long press → deep sleep
  if (gBtn.update(dt)) {
    // Optional: gBle.stop();
    ButtonManager::enterDeepSleep(gLed);
  }

  // Magnet detection
  MagnetState st = gDetector.tick(dt);

  // Update LED/BLE on state change
  static MagnetState lastShown = MagnetState::None;
  if (st != lastShown) {
    lastShown = st;
    gLed.showState(st);
  }
  if (st != gLastSent) {
    gLastSent = st;
    gBle.notify(st);
  }

  delay(20); // ~50 Hz
}
