#include <arduino.h>
#include <pins_arduino.h>
#include "LedDisplay.h"

#undef RGB_BRIGHTNESS
#define RGB_BRIGHTNESS LED_BRIGHTNESS

void LedDisplay::begin() {

	Serial.println("LedDisplay::begin()");

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
	// HACK - RGB Led Reihenfolge stimmt nicht.

	switch (s) {
	case MagnetState::Confirmed: // Green 
		rgbLedWrite(RGB_BUILTIN, RGB_BRIGHTNESS, 0, 0); 
		Serial.println("State:Confirmed");
		break; 
	case MagnetState::Early: // Yellow     
		rgbLedWrite(RGB_BUILTIN, RGB_BRIGHTNESS, RGB_BRIGHTNESS, 0);  
		Serial.println("State:Early");
		break; 
	default: // Red                    
		rgbLedWrite(RGB_BUILTIN, 0, RGB_BRIGHTNESS, 0); 		
		Serial.println("State:None");
		break; 
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
	Serial.println("LED off");
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