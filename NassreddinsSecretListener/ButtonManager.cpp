#include "esp_sleep.h"
#include "ButtonManager.h"


#if CONFIG_IDF_TARGET_ESP32 || CONFIG_IDF_TARGET_ESP32S2 || CONFIG_IDF_TARGET_ESP32S3
// EXT1 verfügbar (mehrere RTC-GPIOs über Bitmaske)
static inline void enable_btn_wakeup(uint8_t pin) {
    esp_sleep_enable_ext1_wakeup(1ULL << pin, ESP_EXT1_WAKEUP_ALL_LOW);
}
#elif CONFIG_IDF_TARGET_ESP32C3 || CONFIG_IDF_TARGET_ESP32H2 || CONFIG_IDF_TARGET_ESP32C6
// Kein EXT1: Deep-Sleep per GPIO-Wakeup (nur bestimmte RTC/LP-GPIOs!)
static inline void enable_btn_wakeup(uint8_t pin) {
    gpio_num_t g = (gpio_num_t)pin;
    // optional prüfen, ob Pin als Wakeup-GPIO erlaubt ist
    if (esp_sleep_is_valid_wakeup_gpio(g)) {           // IDF 5.x
        esp_deep_sleep_enable_gpio_wakeup(g, ESP_GPIO_WAKEUP_GPIO_LOW);
    }
}
#else
#error "Dieses Ziel unterstützt die verwendete Wakeup-Methode nicht."
#endif

void ButtonManager::begin() {
  pinMode(BTN_PIN, INPUT); // GPIO39: input-only, external pullups on board
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
  enable_btn_wakeup(BTN_PIN);
  delay(40);
  esp_deep_sleep_start(); // never returns
}
