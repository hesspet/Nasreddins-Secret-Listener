#include <arduino.h>
#include <pins_arduino.h>
#include "LedDisplay.h"

void LedDisplay::begin() {

#ifdef ARDUINO_LOLIN_C3_PICO
	off();
#else
	FastLED.addLeds<SK6812, ATOM_LED_PIN, GRB>(leds, ATOM_NUM_LEDS);
	FastLED.clear(true);
#endif

}
#ifdef ARDUINO_LOLIN_C3_PICO
void LedDisplay::showState(MagnetState s)
{
	switch (s) {
	case MagnetState::Confirmed: rgbLedWrite(RGB_BUILTIN, 0, RGB_BRIGHTNESS, 0); break; // Green 
	case MagnetState::Early:     rgbLedWrite(RGB_BUILTIN, RGB_BRIGHTNESS, RGB_BRIGHTNESS, 0);  break; // Yellow
	default:                     rgbLedWrite(RGB_BUILTIN, RGB_BRIGHTNESS, 0, 0); break; // Red
	}
}

void LedDisplay::flashBlue(uint8_t times, uint16_t onMs, uint16_t offMs)
{
	for (uint8_t i = 0; i < times; i++) {
		rgbLedWrite(RGB_BUILTIN, 0, 0, RGB_BRIGHTNESS ); // Blau
		delay(onMs);
		off();
		delay(offMs);
	}
}

void LedDisplay::off() 
{
	rgbLedWrite(RGB_BUILTIN, 0, 0, 0); // Off / black 
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