#pragma once

#ifdef ARDUINO_LOLIN_C3_PICO
#else
#include <FastLED.h>
#endif
#include "Config.h"
#include "MagnetState.h"

class LedDisplay
{
public:

	void begin();
	void showState(MagnetState s, bool isConnectedToBleClient);
	void flashBlue(uint8_t times = 2, uint16_t onMs = 120, uint16_t offMs = 100);
	void off();

	void LedRed();
	void LedBlue();
	void LedGreen();
	void LedYellow();
	void LedPink();
	void LedOrange();

private:

#ifndef ARDUINO_LOLIN_C3_PICO
	CRGB leds[ATOM_NUM_LEDS];
#endif
};
