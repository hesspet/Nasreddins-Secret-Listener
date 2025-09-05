#pragma once
#include <NimBLEDevice.h>
#include "Config.h"
#include "MagnetState.h"

class BleService
{
public:

	void begin(const char* deviceName = BLE_DEVICE_NAME);
	void notify(MagnetState s);
	void stop();

private:

	struct ServerCbs : public NimBLEServerCallbacks {
		// NimBLE-Arduino v2.3.x signatures:
		void onConnect(NimBLEServer* pServer, NimBLEConnInfo& connInfo) override;
		void onDisconnect(NimBLEServer* pServer, NimBLEConnInfo& connInfo, int reason) override;

		bool connected = false;
	};

	NimBLEServer* server = nullptr;
	NimBLECharacteristic* notifyChar = nullptr;
	ServerCbs* _serverCbs = nullptr;

public:

	bool isConnected() const {
		return _serverCbs->connected;
	}
};
