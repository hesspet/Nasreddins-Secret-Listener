#include "MagnetDetector.h"

bool MagnetDetector::begin()
{
	Serial.println("MagnetDetector::begin()");

	if (!mag_.begin()) {
		Serial.println("MagnetDetector::ERROR: begin() mag.begin() == false. Chip konnte nicht initialisiert werden.");
		return false;
	}

	// Baseline
	uint32_t t0 = millis(); uint32_t n = 0; double sumB2 = 0;
	while (millis() - t0 < BASELINE_MS) {
		sensors_event_t e; mag_.getEvent(&e);
		float x = e.magnetic.x, y = e.magnetic.y, z = e.magnetic.z;
		sumB2 += (x * x + y * y + z * z); n++;
		delay(10);
	}

	if (n == 0) n = 1;

	B0_sq = (float)(sumB2 / n);
	B0_ut = sqrtf(B0_sq);
	emaB2_slow = emaB2_fast = prevFastB2 = B0_sq;
	recalcThresholds();
	lastLoopMs = lastChangeMs = millis();
	state_ = MagnetState::None;

	return true;
}

void MagnetDetector::recalcThresholds()
{
	DELTA2_ON = 2.0f * B0_ut * DELTA_ON_UT + sqf(DELTA_ON_UT);
	DELTA2_OFF = 2.0f * B0_ut * DELTA_OFF_UT + sqf(DELTA_OFF_UT);
	QUIET_BAND2 = 2.0f * B0_ut * QUIET_BAND_UT + sqf(QUIET_BAND_UT);
	TREND2_ON = 2.0f * B0_ut * TREND_ON_UT + sqf(TREND_ON_UT);
	TREND2_OFF = 2.0f * B0_ut * TREND_OFF_UT + sqf(TREND_OFF_UT);
	EARLY_TARGET2 = EARLY_FRACTION * DELTA2_ON;
}

MagnetState MagnetDetector::tick(uint32_t dtMs)
{
	uint32_t now = millis();

	if (dtMs == 0) dtMs = now - lastLoopMs;

	lastLoopMs = now;

	sensors_event_t e; mag_.getEvent(&e);
	float x = e.magnetic.x, y = e.magnetic.y, z = e.magnetic.z;
	float B2 = x * x + y * y + z * z;

	// Dual-EMA
	emaB2_slow = EMA_ALPHA_B2_SLOW * B2 + (1 - EMA_ALPHA_B2_SLOW) * emaB2_slow;
	emaB2_fast = EMA_ALPHA_B2_FAST * B2 + (1 - EMA_ALPHA_B2_FAST) * emaB2_fast;

	float dB2_slow = emaB2_slow - B0_sq;
	float trendB2 = emaB2_fast - emaB2_slow;
	float slopeB2 = (emaB2_fast - prevFastB2) / (float)dtMs; prevFastB2 = emaB2_fast;
	float predictedDelta2 = (emaB2_slow + slopeB2 * (float)LOOKAHEAD_MS) - B0_sq;

	bool cooldownActive = (now - lastChangeMs) < COOLDOWN_AFTER_HIT_MS;

	// EARLY
	bool earlyCandidate = (!cooldownActive) && ((trendB2 > TREND2_ON) || (predictedDelta2 > EARLY_TARGET2));

	if (!early) {
		if (earlyCandidate) { if (++consecEarlyOn >= DEBOUNCE_EARLY_ON) { early = true; consecEarlyOn = consecEarlyOff = 0; } }
		else consecEarlyOn = 0;
	}
	else {
		if ((trendB2 < TREND2_OFF) && (predictedDelta2 < EARLY_TARGET2 * 0.6f)) {
			if (++consecEarlyOff >= DEBOUNCE_EARLY_OFF) { early = false; consecEarlyOff = 0; }
		}
		else consecEarlyOff = 0;
	}

	// CONFIRMED
	if (!confirmed) {
		if (dB2_slow > DELTA2_ON) {
			if (++consecOn >= DEBOUNCE_ON) {
				confirmed = true; consecOn = consecOff = 0;
				early = true;
				quietAccumMs = 0; lastChangeMs = now;
			}
		}
		else consecOn = 0;
	}
	else {
		if (dB2_slow < DELTA2_OFF) {
			if (++consecOff >= DEBOUNCE_OFF) {
				confirmed = false; consecOff = 0;
				quietAccumMs = 0; lastChangeMs = now;
			}
		}
		else consecOff = 0;
	}

	// State
	MagnetState newState =
		confirmed ? MagnetState::Confirmed :
		(early ? MagnetState::Early : MagnetState::None);

	// Auto recalibration
	bool isQuiet = fabsf(dB2_slow) <= QUIET_BAND2 && !early && !confirmed;
	if (!confirmed && !early && !cooldownActive && isQuiet) {
		quietAccumMs += dtMs;
		if (quietAccumMs >= RECAL_AFTER_MS) {
			B0_sq = (1 - RECAL_ALPHA) * B0_sq + RECAL_ALPHA * emaB2_slow;
			B0_ut = sqrtf(B0_sq);
			recalcThresholds();
			quietAccumMs = 0;
		}
	}
	else {
		quietAccumMs = 0;
	}

	state_ = newState;
	return state_;
}
