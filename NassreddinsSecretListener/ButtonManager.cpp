#include "ButtonManager.h"

void ButtonManager::begin() {
  pinMode(ATOM_BTN_PIN, INPUT); // GPIO39: input-only, external pullups on board
  lastStable=false; debounceMs=0; pressAccum=0; reported=false;
}

bool ButtonManager::update(uint32_t dtMs) {
  bool pressed = readPressed();
  if (pressed != lastStable) {
    debounceMs += dtMs;
    if (debounceMs >= BTN_DEBOUNCE_MS) {
      lastStable = pressed;
      debounceMs = 0;
      if (lastStable) pressAccum = 0;
    }
  } else {
    debounceMs = 0;
  }

  if (lastStable) {
    pressAccum += dtMs;
    if (!reported && pressAccum >= LONG_PRESS_MS) {
      reported = true;
      return true;
    }
  } else {
    pressAccum = 0;
    reported = false;
  }
  return false;
}

void ButtonManager::enterDeepSleep(LedDisplay& led) {
  led.flashBlue(2, 120, 80);
  led.off();
  esp_sleep_enable_ext1_wakeup(1ULL << ATOM_BTN_PIN, ESP_EXT1_WAKEUP_ALL_LOW);
  delay(40);
  esp_deep_sleep_start(); // never returns
}
