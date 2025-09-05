#include <arduino.h>
#include <pins_arduino.h>
#include "LedDisplay.h"

#undef RGB_BRIGHTNESS
#define RGB_BRIGHTNESS LED_BRIGHTNESS

void LedDisplay::begin()
{
	Serial.println("LedDisplay::begin()");

#ifdef ARDUINO_LOLIN_C3_PICO
	off();
#else
	FastLED.addLeds<SK6812, ATOM_LED_PIN, GRB>(leds, ATOM_NUM_LEDS);
	FastLED.clear(true);
#endif
}
#ifdef ARDUINO_LOLIN_C3_PICO
void LedDisplay::showState(MagnetState s, bool isConnectedToBleClient)
{
	// HACK - RGB Led Reihenfolge stimmt nicht.

	switch (s)

	{
	case MagnetState::Confirmed: // Green - Magnet detected
		LedGreen();
		Serial.println("State:Confirmed");
		break;
	case MagnetState::Early: // Yellow
		LedYellow();
		Serial.println("State:Early");
		break;

	default: // Red - Magnet not detected - Pink - Magnet not detected, no BLE connection
		if (isConnectedToBleClient)
		{
			LedPink();
		}
		else
		{
			LedRed();
		}
		Serial.println("State:None");
		break;
	}
}

void LedDisplay::flashBlue(uint8_t times, uint16_t onMs, uint16_t offMs)
{
	for (uint8_t i = 0; i < times; i++)
	{
		LedBlue();
		delay(onMs);
		off();
		delay(offMs);
	}
}

void LedDisplay::off()
{
	Serial.println("LED off");
	rgbLedWrite(RGB_BUILTIN, 0, 0, 0); // Off / black
}

void LedDisplay::LedRed()
{
	Serial.println("LED RED");
	float scale = RGB_BRIGHTNESS / 255.0;
	rgbLedWriteOrdered(RGB_BUILTIN, LED_COLOR_ORDER_RGB, 255 * scale, 0, 0);
}

void LedDisplay::LedBlue()
{
	Serial.println("LED BLUE");
	float scale = RGB_BRIGHTNESS / 255.0;
	rgbLedWriteOrdered(RGB_BUILTIN, LED_COLOR_ORDER_RGB, 0, 0, 255 * scale);
}

void LedDisplay::LedGreen()
{
	Serial.println("LED GREEN");
	float scale = RGB_BRIGHTNESS / 255.0;
	rgbLedWriteOrdered(RGB_BUILTIN, LED_COLOR_ORDER_RGB, 0, 255 * scale, 0);
}

void LedDisplay::LedYellow()
{
	Serial.println("LED YELLOW");
	float scale = RGB_BRIGHTNESS / 255.0;
	rgbLedWriteOrdered(RGB_BUILTIN, LED_COLOR_ORDER_RGB, 255 * scale, 255 * scale, 0);
};

void LedDisplay::LedPink()
{
	Serial.println("LED PINK");
	float scale = RGB_BRIGHTNESS / 255.0;
	rgbLedWriteOrdered(RGB_BUILTIN, LED_COLOR_ORDER_RGB, 255 * scale, 105 * scale, 180 * scale);
}

void LedDisplay::LedOrange()
{
	Serial.println("LED ORANGE");
	float scale = RGB_BRIGHTNESS / 255.0;
	rgbLedWriteOrdered(RGB_BUILTIN, LED_COLOR_ORDER_RGB, 255 * scale, 165 * scale, 0 * scale);
}

#else

void LedDisplay::showState(MagnetState s)
{
	switch (s) {
	case MagnetState::Confirmed: leds[0] = CRGB::Green; break;
	case MagnetState::Early:     leds[0] = CRGB::Yellow; break;
	default:                     leds[0] = CRGB::Red; break;
	}
	FastLED.show();
}

void LedDisplay::flashBlue(uint8_t times, uint16_t onMs, uint16_t offMs)
{
	for (uint8_t i = 0; i < times; i++) {
		leds[0] = CRGB::Blue; FastLED.show(); delay(onMs);
		leds[0] = CRGB::Black; FastLED.show(); delay(offMs);
	}
}

void LedDisplay::off() {
	leds[0] = CRGB::Black; FastLED.show();
}

#endif
