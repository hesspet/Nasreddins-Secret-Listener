#pragma once

// --------- Hardware-Pins (M5Stack Atom Lite) ---------
#define ATOM_LED_PIN      27     // SK6812 data pin
#define ATOM_NUM_LEDS     1
#define ATOM_BTN_PIN      39     // Input-only, active LOW

// --------- BLE (GATT) ---------
#define BLE_DEVICE_NAME   "Nasreddins Secret Listener"
#define BLE_SERVICE_UUID  "6E400001-B5A3-F393-E0A9-E50E24DCCA9E"
#define BLE_NOTIFY_UUID   "6E400003-B5A3-F393-E0A9-E50E24DCCA9E"

// --------- Magnet detection: tuning ---------
#define BASELINE_MS             3000
#define EMA_ALPHA_B2_SLOW       0.12f
#define EMA_ALPHA_B2_FAST       0.45f
#define DELTA_ON_UT             20.0f
#define DELTA_OFF_UT            10.0f
#define TREND_ON_UT             8.0f
#define TREND_OFF_UT            4.0f
#define LOOKAHEAD_MS            250u
#define EARLY_FRACTION          0.70f
#define DEBOUNCE_ON             4
#define DEBOUNCE_OFF            4
#define DEBOUNCE_EARLY_ON       2
#define DEBOUNCE_EARLY_OFF      4
#define QUIET_BAND_UT           5.0f
#define RECAL_AFTER_MS          15000u
#define RECAL_ALPHA             0.05f
#define COOLDOWN_AFTER_HIT_MS   4000u

// --------- Button / DeepSleep ---------
#define LONG_PRESS_MS           7000u
#define BTN_DEBOUNCE_MS         30u
