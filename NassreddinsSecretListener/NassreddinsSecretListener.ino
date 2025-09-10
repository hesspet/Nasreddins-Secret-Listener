/*
================================================================================
  Project: Nasreddin's Secret Listener (ESP32 / M5Stack Atom Lite + HMC5883L)
  Modules: BLE (NimBLE), LED Display (FastLED), Button Manager (DeepSleep),
		   MagnetDetector (B² with dual-EMA, trend + look-ahead, auto-recalib)
  File:    NassreddinsSecretListener.ino   (main wiring)
================================================================================
*/

/*
================================================================================
FUNKTIONSÜBERSICHT
------------------
Diese Firmware erkennt das Annähern und Vorhandensein eines Magneten
unabhängig von der Ausrichtung des Sensors. Zielanwendung ist z. B.
ein Zaubertrick, bei dem ein in der Hand verstecktes Gerät erkennen
soll, ob sich ein Magnet (Ring, Münze o. Ä.) in der Nähe der Hand
des Zuschauers befindet, und diese Erkennung unauffällig an ein
Smartphone über BLE meldet.

Modulaufbau:
  - MagnetDetector: Sensoransteuerung (HMC5883L), Filter, Erkennung, Auto-Rekalibrierung
  - LedDisplay: Ansteuerung der RGB-LED (Atom Lite)
  - BleService: BLE-Server mit Notify-Charakteristik (Status an Smartphone)
  - ButtonManager: Taster-Handling inkl. Long-Press → DeepSleep
  - Hauptprogramm (.ino): Koordination aller Module

Ablauf:
  1. Startphase (Baseline):
	 - Ermittelt über ~3 s die "Grundfeldstärke" des Magnetfelds
	   (Erdfeld + Umgebung) ohne Magnet in der Nähe.
	 - Speichert diesen Wert als B0 (µT) und B0² (µT²).
  2. Messphase:
	 - Liest den HMC5883L mit ca. 50 Hz aus.
	 - Berechnet die Feldstärke B² = x² + y² + z².
	 - Führt zwei Filter:
		 * SLOW-EMA: bildet langsam den Hintergrund ab.
		 * FAST-EMA: reagiert schnell auf Änderungen (Annäherung).
	 - Ermittelt Trend = FAST − SLOW.
	 - Errechnet eine Vorhersage (Look-Ahead), ob in Kürze eine
	   Erkennungsschwelle erreicht wird.
  3. Statuslogik:
	 - EARLY: Magnet naht (starker Trend oder Look-Ahead nahe Schwelle).
	 - CONFIRMED: Magnet sicher erkannt (absolute Schwelle über Baseline).
	 - Debounce-Zähler sorgen für stabile Erkennung.
	 - Auto-Rekalibrierung passt Baseline an, wenn lange kein Magnet da ist.
  4. Ausgabe / Feedback:
	 - RGB-LED des Atom Lite zeigt Status:
		 * Rot   = kein Magnet (NONE)
		 * Gelb  = Magnet naht (EARLY)
		 * Grün  = Magnet erkannt (CONFIRMED)
	 - BLE-Notify sendet Status als Byte an verbundenes Smartphone.
  5. Stromsparmodus:
	 - 7 s Long-Press auf den Atom-Button → LED blinkt blau → DeepSleep.
	 - Aufwecken durch erneuten Tastendruck.

Wichtige Funktionen:
  - setup(): Initialisiert Sensor, LED, BLE, Button und ermittelt Baseline.
  - loop():
	  * ButtonManager::update(): prüft Long-Press → ggf. DeepSleep.
	  * MagnetDetector::tick(): aktualisiert Filter und Status.
	  * LedDisplay::showState(): setzt LED passend zum Status.
	  * BleService::notify(): sendet Status an Smartphone.
  - MagnetDetector::recalcThresholds(): berechnet alle Schwellwerte aus Baseline.
  - ButtonManager::enterDeepSleep(): versetzt System in tiefsten Schlaf.

Anpassbare Parameter (siehe Config.h):
  - BASELINE_MS: Dauer der Grundfeld-Ermittlung.
  - EMA_ALPHA_B2_SLOW / EMA_ALPHA_B2_FAST: Glättungsfaktoren für Filter.
  - DELTA_ON_UT / DELTA_OFF_UT: Hauptschwellen für CONFIRMED.
  - TREND_ON_UT / TREND_OFF_UT: Trendschwellen für EARLY.
  - LOOKAHEAD_MS / EARLY_FRACTION: Vorhersagefenster für frühe Erkennung.
  - QUIET_BAND_UT / RECAL_AFTER_MS: Bedingungen für Auto-Rekalibrierung.
  - LONG_PRESS_MS: Dauer für DeepSleep-Auslösung per Button.
================================================================================
*/

#include <NimBLEDevice.h>
#include <Wire.h>
#include <Adafruit_Sensor.h>
#include <Adafruit_HMC5883_U.h>

#include "Config.h"
#include "MagnetState.h"
#include "LedDisplay.h"
#include "BleService.h"
#include "ButtonManager.h"
#include "MagnetDetector.h"

// --- Modules ---

Adafruit_HMC5883_Unified gMag(12345);
LedDisplay    gLed;
BleService    gBle;
ButtonManager gBtn;
MagnetDetector gDetector(gMag);

// --- Runtime ---

uint32_t gLastMs = 0;
MagnetState gLastSent = MagnetState::None;

void setup()
{
	Serial.begin(115200);
	delay(100);

#ifdef ARDUINO_LOLIN_C3_PICO
	Serial.println("Ermitteltes Board: LOLIN_C3_PICO");
#else
	Serial.println("Unbekanntes Board"); // ARDUINO_M5ATOM
#endif

	gLed.begin();
	delay(100);
	gLed.LedOrange(); delay(500);

	gBtn.begin();
	gLed.LedPink(); delay(000);

	gLed.LedBlue();
	gBle.begin(BLE_DEVICE_NAME);

	// TODO bei Störung Orange blinken

	if (!gDetector.begin()) {
		Serial.println("ERROR: Magnetometer not found!");
		gLed.flashBlue(3, 80, 80);
		while (1) delay(200);
	}

	// einfach nur zur "show"
	gLed.LedYellow(); delay(500);
	gLed.LedRed(); delay(500);

	gLed.showState(MagnetState::None, gBle.isConnected());

	gBle.notify(MagnetState::Early);
	gBle.notify(MagnetState::None);

	gLastMs = millis();

	Serial.println("start loop() - now showing states...");
}

void loop()
{
	uint32_t now = millis();
	uint32_t dt = now - gLastMs; if (dt == 0) dt = 1; gLastMs = now;

#ifdef FEATURE_DEEPSLEEP
	// Button: 7s long press → deep sleep
	if (gBtn.update(dt)) {
		gBle.stop();
		Serial.println("enterDeepSleep...");
		ButtonManager::enterDeepSleep(gLed);
	}
#endif

	// Magnet detection
	MagnetState st = gDetector.tick(dt);

	// Update LED/BLE on state change
	static MagnetState lastShown = MagnetState::None;
	static bool lastIsConnection = false;
	if (st != lastShown || lastIsConnection != gBle.isConnected()) {
		lastShown = st;
		lastIsConnection = gBle.isConnected();
		gLed.showState(lastShown, lastIsConnection);
	}

	if (st != gLastSent) {
		gLastSent = st;
		gBle.notify(st);
	}

	delay(20); // ~50 Hz
}
