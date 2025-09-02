#pragma once
#include <Arduino.h>
#include <esp_sleep.h>
#include "Config.h"
#include "LedDisplay.h"

class ButtonManager {
public:
  void begin();
  // call periodically with dt (ms); returns true ONCE when long-press detected
  bool update(uint32_t dtMs);
  // DeepSleep with wake via button (LOW)
  static void enterDeepSleep(LedDisplay& led);
private:
  bool lastStable=false;
  uint32_t debounceMs=0;
  uint32_t pressAccum=0;
  bool reported=false;

  inline bool readPressed() { return digitalRead(BTN_PIN) == LOW; } // active LOW
};
