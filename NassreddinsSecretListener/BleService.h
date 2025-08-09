#pragma once
#include <NimBLEDevice.h>
#include "Config.h"
#include "LedDisplay.h" // for MagnetState enum

class BleService {
public:
  void begin(const char* deviceName = BLE_DEVICE_NAME);
  void notify(MagnetState s);
  void stop(); // optional
private:
  struct ServerCbs : public NimBLEServerCallbacks {
    // NimBLE-Arduino v2.3.x signatures:
    void onConnect(NimBLEServer* pServer, NimBLEConnInfo& connInfo) override;
    void onDisconnect(NimBLEServer* pServer, NimBLEConnInfo& connInfo, int reason) override;
  };
  NimBLEServer* server = nullptr;
  NimBLECharacteristic* notifyChar = nullptr;
};
