/*
===============================================================================
  Projekt: Magnet-Erkennung mit M5Stack Atom Lite + HMC5883L
  Autor:   Peter Heß
  Datum:   2025-08-09
  File:    D:\dev\Nasreddins-Secret-Listener\AtticAndTestversions\MagnetometerTest1\Magnetomter_Test\\Magnetomter_Test.ino
  Device:  M5STACK ATOM Lite
===============================================================================

  FUNKTIONSÜBERSICHT
  ------------------
  Diese Firmware erkennt das Annähern und Vorhandensein eines Magneten
  unabhängig von der Ausrichtung des Sensors. Zielanwendung ist z. B.
  ein Zaubertrick, bei dem ein in der Hand verstecktes Gerät erkennen
  soll, ob sich ein Magnet (Ring, Münze, o. Ä.) in der Nähe der Hand
  des Zuschauers befindet.

  Ablauf:
    1. Startphase (Baseline):
       - Ermittelt über ~3 Sekunden die "Grundfeldstärke" des Magnetfelds
         (Erdfeld + Umgebung) ohne Magnet in der Nähe.
       - Speichert diesen Wert als B0 (µT) und B0² (µT²).
    2. Messphase:
       - Liest den HMC5883L in 50 Hz aus.
       - Berechnet die Feldstärke B² = x² + y² + z².
       - Führt zwei Filter:
           * SLOW-EMA: bildet langsam den Hintergrund ab.
           * FAST-EMA: reagiert schnell auf Änderungen (Annäherung).
       - Ermittelt Trend = FAST - SLOW.
       - Errechnet Vorhersage (Look-Ahead), ob in kurzer Zeit eine
         Erkennungsschwelle erreicht wird.
    3. Statuslogik:
       - EARLY: Magnet naht (starker Trend oder Look-Ahead nahe Schwelle).
       - CONFIRMED: Magnet sicher erkannt (absolute Schwelle über Baseline).
       - Debounce sorgt für stabile Erkennung.
       - Auto-Rekalibrierung passt Baseline an, wenn lange kein Magnet da ist.
    4. Ausgabe:
       - RGB-LED des Atom Lite zeigt Status:
           * Rot   = kein Magnet
           * Gelb  = Magnet naht (EARLY)
           * Grün  = Magnet erkannt (CONFIRMED)
       - Serielle Debug-Ausgabe optional.

  Wichtige Funktionen:
    - setup(): Initialisiert Sensor, LED, ermittelt Baseline.
    - loop(): Liest Sensor, aktualisiert Filter, prüft Status,
              steuert LED und ggf. Rekalibrierung.
    - updateLED(): Setzt LED-Farbe passend zum Status.
    - recalcThresholdsFromBaseline(): Berechnet alle Schwellwerte aus Baseline.

  Anpassbare Parameter (oben im Code):
    - BASELINE_MS: Dauer der Grundfeld-Ermittlung.
    - EMA_ALPHA_B2_SLOW / FAST: Glättung für Filter.
    - DELTA_ON_UT / DELTA_OFF_UT: Hauptschwellen für CONFIRMED.
    - TREND_ON_UT / TREND_OFF_UT: Trendschwellen für EARLY.
    - LOOKAHEAD_MS / EARLY_FRACTION: Vorhersagefenster für frühe Erkennung.
    - QUIET_BAND_UT / RECAL_AFTER_MS: Bedingungen für Auto-Rekalibrierung.
===============================================================================
*/
#include <Wire.h>
#include <Adafruit_Sensor.h>
#include <Adafruit_HMC5883_U.h>
#include <FastLED.h>  // Bibliothek für WS2812-kompatible RGB-LEDs

// --- Magnetometer-Objekt (Adafruit Unified Sensor API) ---
Adafruit_HMC5883_Unified mag = Adafruit_HMC5883_Unified(12345);

// --- LED-Einstellungen für M5 Atom Lite ---
// Atom Lite hat 1 eingebaute SK6812 RGB-LED auf Pin 27
#define LED_PIN     27
#define NUM_LEDS    1
CRGB leds[NUM_LEDS];  // FastLED-Array mit 1 LED

// -----------------------------------------------------------------------------
// EINSTELLUNGEN ZUR ERKENNUNG
// -----------------------------------------------------------------------------

// Dauer beim Start, um die Ausgangslage ("Baseline") ohne Magnet zu ermitteln
const uint16_t BASELINE_MS       = 3000;

// Glättungsfaktoren (Exponentieller gleitender Mittelwert) für B²:
// - SLOW: träger, bildet den Hintergrund / Grundfeldstärke
// - FAST: reagiert schnell auf Änderungen (Magnet nähert sich)
const float    EMA_ALPHA_B2_SLOW = 0.12f;
const float    EMA_ALPHA_B2_FAST = 0.45f;

// Absolute Schwellwerte in µT über der Baseline
const float    DELTA_ON_UT       = 20.0f; // "CONFIRMED" an
const float    DELTA_OFF_UT      = 10.0f; // "CONFIRMED" aus

// Früherkennung basierend auf Trend (Annäherungsgeschwindigkeit)
const float    TREND_ON_UT       = 8.0f;  // EARLY an
const float    TREND_OFF_UT      = 4.0f;  // EARLY aus

// Look-Ahead: Vorhersage, ob wir bald die Hauptschwelle erreichen
const uint32_t LOOKAHEAD_MS      = 250;   // in Millisekunden
const float    EARLY_FRACTION    = 0.70f; // Anteil der Hauptschwelle für EARLY

// Debounce-Werte (wie viele Messungen in Folge nötig sind)
const int      DEBOUNCE_ON       = 4;  // für CONFIRMED
const int      DEBOUNCE_OFF      = 4;
const int      DEBOUNCE_EARLY_ON  = 2; // für EARLY
const int      DEBOUNCE_EARLY_OFF = 4;

// Auto-Rekalibrierung (langsam driftende Baseline anpassen)
const float    QUIET_BAND_UT     = 5.0f;   // µT-Toleranz als "ruhig"
const uint32_t RECAL_AFTER_MS    = 15000;  // Zeit in Ruhe bis Rekalibrierung
const float    RECAL_ALPHA       = 0.05f;  // Schrittweite der Rekalibrierung
const uint32_t COOLDOWN_AFTER_HIT_MS = 4000; // Sperre nach Erkennung

// -----------------------------------------------------------------------------
// LAUFVARIABLEN (werden im Betrieb verändert)
// -----------------------------------------------------------------------------

float B0_sq   = 0.0f;   // Baseline: Feldstärke B² (µT²)
float B0_ut   = 0.0f;   // Baseline: Feldstärke B   (µT) (nur für Schwellenberechnung)

float emaB2_slow = 0.0f; // SLOW-Filter für B² (Hintergrund)
float emaB2_fast = 0.0f; // FAST-Filter für B² (Annäherung)
float prevFastB2 = 0.0f; // Vorheriger Wert für Steigungsberechnung

bool confirmed = false; // Status "Magnet sicher erkannt"
bool early     = false; // Status "Magnet naht"

int consecOn = 0, consecOff = 0;             // Zähler für CONFIRMED-Debounce
int consecEarlyOn = 0, consecEarlyOff = 0;   // Zähler für EARLY-Debounce

// Dynamische Schwellenwerte in µT² (werden aus Baseline berechnet)
float DELTA2_ON = 0.0f;  
float DELTA2_OFF = 0.0f;
float QUIET_BAND2 = 0.0f;
float TREND2_ON = 0.0f;
float TREND2_OFF = 0.0f;
float EARLY_TARGET2 = 0.0f; // EARLY-Schwelle (µT²)

// Zeitstempel und Zähler
uint32_t lastChangeMs = 0; // Zeitpunkt letzter Statusänderung
uint32_t quietAccumMs = 0; // Akkumulierte "ruhige" Zeit (für Rekalibrierung)
uint32_t lastLoopMs   = 0; // Zeitpunkt letzter loop()-Iteration

// -----------------------------------------------------------------------------
// HILFSFUNKTIONEN
// -----------------------------------------------------------------------------

// Schneller Quadrathelfer
static inline float sqf(float v) { return v * v; }

// LED entsprechend Status aktualisieren
void updateLED() {
  if (confirmed) {
    leds[0] = CRGB::Green;  // Grün = Magnet sicher erkannt
  } else if (early) {
    leds[0] = CRGB::Yellow; // Gelb = Magnet naht
  } else {
    leds[0] = CRGB::Red;    // Rot = kein Magnet
  }
  FastLED.show();
}

// Schwellenwerte (in µT²) aus Baseline (in µT) berechnen
void recalcThresholdsFromBaseline() {
  // Δ² = (B0 + Δ)^2 - B0^2 = 2*B0*Δ + Δ^2
  DELTA2_ON   = 2.0f * B0_ut * DELTA_ON_UT  + sqf(DELTA_ON_UT);
  DELTA2_OFF  = 2.0f * B0_ut * DELTA_OFF_UT + sqf(DELTA_OFF_UT);
  QUIET_BAND2 = 2.0f * B0_ut * QUIET_BAND_UT + sqf(QUIET_BAND_UT);

  TREND2_ON   = 2.0f * B0_ut * TREND_ON_UT  + sqf(TREND_ON_UT);
  TREND2_OFF  = 2.0f * B0_ut * TREND_OFF_UT + sqf(TREND_OFF_UT);

  EARLY_TARGET2 = EARLY_FRACTION * DELTA2_ON;
}

// -----------------------------------------------------------------------------
// SETUP
// -----------------------------------------------------------------------------
void setup() {
  Serial.begin(115200);
  delay(100);

  // LED initialisieren
  FastLED.addLeds<SK6812, LED_PIN, GRB>(leds, NUM_LEDS);
  FastLED.clear(true);

  // Sensor starten
  if (!mag.begin()) {
    Serial.println("Kein HMC5883 erkannt!");
    while (1) delay(100);
  }

  // Baseline ermitteln (Magnet fernhalten)
  uint32_t t0 = millis();
  uint32_t n = 0;
  double sumB2 = 0.0;

  while (millis() - t0 < BASELINE_MS) {
    sensors_event_t e;
    mag.getEvent(&e);
    float x = e.magnetic.x, y = e.magnetic.y, z = e.magnetic.z;
    float B2 = x*x + y*y + z*z;
    sumB2 += B2; n++;
    delay(10);
  }
  if (n == 0) n = 1;
  B0_sq = (float)(sumB2 / n);
  B0_ut = sqrtf(B0_sq);

  emaB2_slow = B0_sq;
  emaB2_fast = B0_sq;
  prevFastB2 = B0_sq;

  recalcThresholdsFromBaseline();

  lastLoopMs = millis();
  lastChangeMs = millis();
}

// -----------------------------------------------------------------------------
// LOOP
// -----------------------------------------------------------------------------
void loop() {
  uint32_t now = millis();
  uint32_t dt  = now - lastLoopMs;
  if (dt == 0) dt = 1;
  lastLoopMs = now;

  // Magnetometer auslesen
  sensors_event_t e;
  mag.getEvent(&e);
  float x = e.magnetic.x, y = e.magnetic.y, z = e.magnetic.z;
  float B2 = x*x + y*y + z*z; // Feldstärke B²

  // EMA-Filter aktualisieren
  emaB2_slow = EMA_ALPHA_B2_SLOW * B2 + (1.0f - EMA_ALPHA_B2_SLOW) * emaB2_slow;
  emaB2_fast = EMA_ALPHA_B2_FAST * B2 + (1.0f - EMA_ALPHA_B2_FAST) * emaB2_fast;

  // Abweichungen zur Baseline
  float dB2_slow = emaB2_slow - B0_sq;

  // Trend (schneller Pfad über langsamen)
  float trendB2 = emaB2_fast - emaB2_slow;

  // Steigung (für Look-Ahead)
  float slopeB2 = (emaB2_fast - prevFastB2) / (float)dt;
  prevFastB2 = emaB2_fast;

  // Vorhersage: Wo liegt SLOW in LOOKAHEAD_MS?
  float predictedSlowB2 = emaB2_slow + slopeB2 * (float)LOOKAHEAD_MS;
  float predictedDelta2 = predictedSlowB2 - B0_sq;

  // EARLY-Logik
  bool cooldownActive = (now - lastChangeMs) < COOLDOWN_AFTER_HIT_MS;
  bool earlyCandidate =
      (!cooldownActive) &&
      ( (trendB2 > TREND2_ON) || (predictedDelta2 > EARLY_TARGET2) );

  if (!early) {
    if (earlyCandidate) {
      if (++consecEarlyOn >= DEBOUNCE_EARLY_ON) {
        early = true;
        consecEarlyOn = consecEarlyOff = 0;
      }
    } else consecEarlyOn = 0;
  } else {
    if ( (trendB2 < TREND2_OFF) && (predictedDelta2 < EARLY_TARGET2 * 0.6f) ) {
      if (++consecEarlyOff >= DEBOUNCE_EARLY_OFF) {
        early = false;
        consecEarlyOff = 0;
      }
    } else consecEarlyOff = 0;
  }

  // CONFIRMED-Logik
  if (!confirmed) {
    if (dB2_slow > DELTA2_ON) {
      if (++consecOn >= DEBOUNCE_ON) {
        confirmed = true;
        consecOn = consecOff = 0;
        early = true; // CONFIRMED impliziert auch EARLY
        quietAccumMs = 0;
        lastChangeMs = now;
      }
    } else consecOn = 0;
  } else {
    if (dB2_slow < DELTA2_OFF) {
      if (++consecOff >= DEBOUNCE_OFF) {
        confirmed = false;
        consecOff = 0;
        quietAccumMs = 0;
        lastChangeMs = now;
      }
    } else consecOff = 0;
  }

  // Auto-Rekalibrierung, wenn lange ruhig
  bool isQuiet = fabsf(dB2_slow) <= QUIET_BAND2 && !early && !confirmed;
  if (!confirmed && !early && !cooldownActive && isQuiet) {
    quietAccumMs += dt;
    if (quietAccumMs >= RECAL_AFTER_MS) {
      B0_sq = (1.0f - RECAL_ALPHA) * B0_sq + RECAL_ALPHA * emaB2_slow;
      B0_ut = sqrtf(B0_sq);
      recalcThresholdsFromBaseline();
      quietAccumMs = 0;
    }
  } else if (!isQuiet) {
    quietAccumMs = 0;
  }

  // LED-Status setzen
  updateLED();

  delay(20); // Abtastrate ~50 Hz
}
