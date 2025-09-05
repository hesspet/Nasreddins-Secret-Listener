#pragma once

// Device state for LED & BLE
enum class MagnetState : uint8_t {
	None = 0x00,
	Early = 0x01,
	Confirmed = 0x02
};
