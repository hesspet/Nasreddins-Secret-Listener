#pragma once
#include <Adafruit_Sensor.h>
#include <Adafruit_HMC5883_U.h>
#include "Config.h"
#include "LedDisplay.h" // MagnetState

class MagnetDetector {
public:
  explicit MagnetDetector(Adafruit_HMC5883_Unified& mag): mag_(mag) {}

  bool begin();                     // init & baseline
  MagnetState tick(uint32_t dtMs);  // call periodically; returns current state
  MagnetState state() const { return state_; }

private:
  Adafruit_HMC5883_Unified& mag_;
  float B0_sq=0, B0_ut=0, emaB2_slow=0, emaB2_fast=0, prevFastB2=0;
  float DELTA2_ON=0, DELTA2_OFF=0, QUIET_BAND2=0, TREND2_ON=0, TREND2_OFF=0, EARLY_TARGET2=0;
  bool confirmed=false, early=false;
  int  consecOn=0, consecOff=0, consecEarlyOn=0, consecEarlyOff=0;
  uint32_t lastChangeMs=0, quietAccumMs=0, lastLoopMs=0;
  MagnetState state_ = MagnetState::None;

  inline static float sqf(float v){ return v*v; }
  void recalcThresholds();
};
