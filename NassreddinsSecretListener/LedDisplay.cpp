#include "LedDisplay.h"

void LedDisplay::begin() {
  FastLED.addLeds<SK6812, ATOM_LED_PIN, GRB>(leds, ATOM_NUM_LEDS);
  FastLED.clear(true);
}
void LedDisplay::showState(MagnetState s) {
  switch (s) {
    case MagnetState::Confirmed: leds[0] = CRGB::Green; break;
    case MagnetState::Early:     leds[0] = CRGB::Yellow; break;
    default:                     leds[0] = CRGB::Red; break;
  }
  FastLED.show();
}
void LedDisplay::flashBlue(uint8_t times, uint16_t onMs, uint16_t offMs) {
  for (uint8_t i=0;i<times;i++) {
    leds[0] = CRGB::Blue; FastLED.show(); delay(onMs);
    leds[0] = CRGB::Black; FastLED.show(); delay(offMs);
  }
}
void LedDisplay::off() { leds[0] = CRGB::Black; FastLED.show(); }
