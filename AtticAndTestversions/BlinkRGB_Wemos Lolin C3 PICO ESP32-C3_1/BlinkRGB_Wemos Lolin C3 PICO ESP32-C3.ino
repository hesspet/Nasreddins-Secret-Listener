/*
  BlinkRGB

  Demonstrates usage of onboard RGB LED on some ESP dev boards.

  Calling digitalWrite(RGB_BUILTIN, HIGH) will use hidden RGB driver.
    
  RGBLedWrite demonstrates controll of each channel:
  void neopixelWrite(uint8_t pin, uint8_t red_val, uint8_t green_val, uint8_t blue_val)

  WARNING: After using digitalWrite to drive RGB LED it will be impossible to drive the same pin
    with normal HIGH/LOW level

    Quelle: https://github.com/espressif/arduino-esp32/blob/2.0.14/libraries/ESP32/examples/GPIO/BlinkRGB/BlinkRGB.ino
*/ 

#undef RGB_BRIGHTNESS
#define RGB_BRIGHTNESS 32 // Change white brightness (max 255)
#define DELAY 500

void setup() {

}


void loop() {

#ifdef RGB_BUILTIN
  // digitalWrite(RGB_BUILTIN, HIGH);   // Turn the RGB LED white
  // delay(1000);
  digitalWrite(RGB_BUILTIN, LOW);    // Turn the RGB LED off
  delay(DELAY);

  rgbLedWrite(RGB_BUILTIN,RGB_BRIGHTNESS,0,0); // Red
  delay(DELAY);
  rgbLedWrite(RGB_BUILTIN,0,RGB_BRIGHTNESS,0); // Green
  delay(DELAY);
  rgbLedWrite(RGB_BUILTIN,0,0,RGB_BRIGHTNESS); // Blue
  delay(DELAY);
  rgbLedWrite(RGB_BUILTIN,0,0,0); // Off / black
  delay(DELAY);
#endif

}