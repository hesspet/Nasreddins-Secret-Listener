#include "BleService.h"

void BleService::begin(const char* deviceName) {
  NimBLEDevice::init(deviceName);
  NimBLEDevice::setPower(ESP_PWR_LVL_P7);
  NimBLEDevice::setSecurityAuth(false, false, true);

  server = NimBLEDevice::createServer();
  server->setCallbacks(new ServerCbs());

  auto* svc = server->createService(BLE_SERVICE_UUID);
  notifyChar = svc->createCharacteristic(BLE_NOTIFY_UUID, NIMBLE_PROPERTY::NOTIFY);
  svc->start();

  auto* adv = NimBLEDevice::getAdvertising();
  adv->addServiceUUID(BLE_SERVICE_UUID);
  adv->setName(deviceName);           // ensure name advertised
  adv->enableScanResponse(true);      // API in 2.x (setScanResponse -> enableScanResponse)
  adv->start();
}

// Callbacks implementations
void BleService::ServerCbs::onConnect(NimBLEServer* pServer, NimBLEConnInfo& connInfo) {
  // Optional: continue advertising to allow reconnects/others
  NimBLEDevice::getAdvertising()->start();
}
void BleService::ServerCbs::onDisconnect(NimBLEServer* pServer, NimBLEConnInfo& connInfo, int reason) {
  NimBLEDevice::getAdvertising()->start();
}

void BleService::notify(MagnetState s) {
  if (!notifyChar) return;
  uint8_t v = static_cast<uint8_t>(s);
  notifyChar->setValue(&v, 1);
  notifyChar->notify();
}

void BleService::stop() {
  if (server) server->stopAdvertising(); // correct API; server->stop() does not exist
  NimBLEDevice::deinit(true);
}
