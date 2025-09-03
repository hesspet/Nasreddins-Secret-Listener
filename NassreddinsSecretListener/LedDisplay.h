#pragma once

#ifdef ARDUINO_LOLIN_C3_PICO
#else
#include <FastLED.h>
#endif
#include "Config.h"

// Device state for LED & BLE
enum class MagnetState : uint8_t { 
  None=0x00, 
  Early=0x01, 
  Confirmed=0x02 
  };

class LedDisplay {

public:
  void begin();
  void showState(MagnetState s);
  void flashBlue(uint8_t times=2, uint16_t onMs=120, uint16_t offMs=100);
  void off();

private:
#ifndef ARDUINO_LOLIN_C3_PICO
  CRGB leds[ATOM_NUM_LEDS];
#endif
};
