# Klassen

```mermaid
%% Nasreddins Secret Listener – Klassenübersicht
classDiagram
    direction LR

    class MagnetState {
      <<enum>>
      +None
      +Early
      +Confirmed
    }

    class LedDisplay {
      +void begin()
      +void showState(MagnetState s)
      +void flashBlue(times=2, onMs=120, offMs=100)
      +void off()
    }

    class BleService {
      +void begin(deviceName="Nasreddins Secret Listener")
      +void notify(MagnetState s)
      +void stop()
      --
      -NimBLEServer* server
      -NimBLECharacteristic* notifyChar
      <<uses NimBLE-Arduino>>
    }

    class ButtonManager {
      +void begin()
      +bool update(dtMs)
      +static void enterDeepSleep(LedDisplay& led)
      --
      -bool lastStable
      -uint32_t debounceMs
      -uint32_t pressAccum
      -bool reported
    }

    class MagnetDetector {
      +bool begin()
      +MagnetState tick(dtMs)
      +MagnetState state()
      --
      -Adafruit_HMC5883_Unified& mag
      -float B0_sq, B0_ut
      -float emaB2_slow, emaB2_fast, prevFastB2
      -float DELTA2_ON, DELTA2_OFF, QUIET_BAND2
      -float TREND2_ON, TREND2_OFF, EARLY_TARGET2
      -bool confirmed, early
      -int consecOn, consecOff, consecEarlyOn, consecEarlyOff
      -uint32_t lastChangeMs, quietAccumMs, lastLoopMs
      -MagnetState state_
      <<uses Adafruit_HMC5883>>
    }

    ```
    class Config_h {
      <<config>>
      BASELINE_MS
      EMA_ALPHA_B2_SLOW/FAST
      DELTA_ON_UT/DELTA_OFF_UT
      TREND_ON_UT/TREND_OFF_UT
      LOOKAHEAD_MS/EARLY_FRACTION
      QUIET_BAND_UT/RECAL_AFTER_MS/RECAL_ALPHA
      COOLDOWN_AFTER_HIT_MS
      LONG_PRESS_MS/BTN_DEBOUNCE_MS
      ATOM_LED_PIN/ATOM_BTN_PIN
      BLE_SERVICE_UUID/BLE_NOTIFY_UUID
      BLE_DEVICE_NAME
    }

    class MainIno {
      <<.ino>>
      -Adafruit_HMC5883_Unified gMag
      -LedDisplay gLed
      -BleService gBle
      -ButtonManager gBtn
      -MagnetDetector gDetector
      -uint32_t gLastMs
      -MagnetState gLastSent
      +setup()
      +loop()
    }

    %% Beziehungen
    MainIno --> LedDisplay : verwendet
    MainIno --> BleService : verwendet
    MainIno --> ButtonManager : verwendet
    MainIno --> MagnetDetector : verwendet
    MagnetDetector --> MagnetState
    LedDisplay --> MagnetState
    BleService --> MagnetState
    MainIno ..> Config_h : Konstanten
```

## Sequenzablauf

```mermaid
%% Nasreddins Secret Listener – Sequenzablauf
sequenceDiagram
    autonumber
    participant INO as NassreddinsSecretListener.ino
    participant BTN as ButtonManager
    participant LED as LedDisplay
    participant DET as MagnetDetector
    participant BLE as BleService

    rect rgb(245,245,245)
    note over INO: setup()
    INO->>LED: begin()
    INO->>BTN: begin()
    INO->>BLE: begin(BLE_DEVICE_NAME)
    INO->>DET: begin()  <!-- Baseline lernen (~3s), Thresholds berechnen -->
    DET-->>INO: ok / fail
    INO->>LED: showState(None)
    INO->>BLE: notify(None)
    end

    loop loop() ~50 Hz
      INO->>BTN: update(dtMs)
      BTN-->>INO: longPress? (true/false)
      alt longPress==true
        INO->>BTN: enterDeepSleep(LED)
        Note over INO: Deep Sleep (Wake by Button)
      else
        INO->>DET: tick(dtMs)
        DET-->>INO: state (None/Early/Confirmed)
        INO->>LED: showState(state) (nur bei Änderung)
        INO->>BLE: notify(state) (nur bei Änderung)
      end
    end

```

##  Zustandsautomat MagnetDetector

```mermaid
%% Nasreddins Secret Listener – Zustandsautomat MagnetDetector
stateDiagram-v2
    [*] --> None

    state "None\n(Baseline, ruhig)" as None
    state "Early\n(Trend↑ / LookAhead)" as Early
    state "Confirmed\n(Δ² > ΔON² + Debounce)" as Confirmed

    None --> Early: trendB2 > TREND2_ON\nOR predictedΔ2 > EARLY_TARGET2\n(+ Debounce)
    Early --> None: trendB2 < TREND2_OFF AND predictedΔ2 < 0.6*EARLY_TARGET2\n(+ Debounce)

    Early --> Confirmed: dB2_slow > DELTA2_ON\n(+ Debounce)
    Confirmed --> Early: dB2_slow < DELTA2_OFF\n(+ Debounce)

    note right of None
      Auto-Rekalibrierung läuft hier,
      wenn lange ruhig & kein Cooldown.
    end note

```

## BLE-Advertising-Ablauf mit Manufacturer Data

```mermaid
sequenceDiagram
    autonumber
    participant ESP as ESP32<br/>(Nasreddin's Secret Listener)
    participant ADV as BLE Advertising
    participant APP as Smartphone-App<br/>(MAUI)
    participant USER as Zauberer

    rect rgb(245,245,245)
    note over ESP: Startup / BLE-Init
    ESP->>ESP: getBleMac()
    ESP->>ESP: shortId = last 3 bytes of MAC<br/>e.g. "AB12CD"
    ESP->>ADV: Set device name = "NSL-AB12CD"
    ESP->>ADV: Set manufacturer data =<br/>[2B ManufID][6B MAC][1B Status]
    ADV->>ADV: enableScanResponse(true)
    ADV->>ADV: start()
    end

    rect rgb(240,255,240)
    note over APP: Scanning nach BLE_SERVICE_UUID
    APP->>ADV: Scan request
    ADV-->>APP: Advertising data<br/>(Name, UUIDs, Manufacturer Data)
    APP->>APP: Parse manufacturer data<br/>→ Extract MAC & Status
    APP->>APP: Vergleiche mit gespeicherter MAC<br/>aus "Taufe"
    alt MAC match
        APP->>USER: (optional) Anzeige "Gerät gefunden"
        APP->>ESP: Connect + Subscribe NotifyChar
    else MAC mismatch
        APP->>APP: Ignorieren
    end
    end

