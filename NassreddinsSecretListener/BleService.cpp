#include "BleService.h"

void BleService::begin(const char* deviceName)
{
	Serial.println("BleService::begin()");

	NimBLEDevice::init(deviceName);
#ifdef ESP_PWR_LVL_P7
	NimBLEDevice::setPower(ESP_PWR_LVL_P7); // bei Atom
#else
	NimBLEDevice::setPower(ESP_PWR_LVL_P6); // Bei C3 Wemos Lolin nur P6 erlaubt.
#endif

	NimBLEDevice::setSecurityAuth(false, false, true);

	server = NimBLEDevice::createServer();
	_serverCbs = new ServerCbs();
	server->setCallbacks(_serverCbs);

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
void BleService::ServerCbs::onConnect(NimBLEServer* pServer, NimBLEConnInfo& connInfo)
{
	Serial.println("BleService::onConnect()");

	// Optional: continue advertising to allow reconnects/others
	NimBLEDevice::getAdvertising()->start();
	connected = true;
}
void BleService::ServerCbs::onDisconnect(NimBLEServer* pServer, NimBLEConnInfo& connInfo, int reason)
{
	Serial.println("BleService::onDisconnect()");

	connected = false;
	NimBLEDevice::getAdvertising()->start();
}

void BleService::notify(MagnetState s) {
	if (!notifyChar) return;

	Serial.println("BleService::notify()");

	uint8_t v = static_cast<uint8_t>(s);
	notifyChar->setValue(&v, 1);
	notifyChar->notify();
}

void BleService::stop()
{
	Serial.println("BleService::stop()");

	if (server) server->stopAdvertising();
	NimBLEDevice::deinit(true);
}
