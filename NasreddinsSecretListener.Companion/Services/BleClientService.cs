using NasreddinsSecretListener.Companion.Helper;
using NasreddinsSecretListener.Companion.Models;
using Plugin.BLE;
using Plugin.BLE.Abstractions;
using Plugin.BLE.Abstractions.Contracts;
using Plugin.BLE.Abstractions.EventArgs;
using static AndroidX.ConstraintLayout.Core.Motion.Utils.HyperSpline;
using Microsoft.Maui.Storage;   // Preferences
using Microsoft.Maui.Devices;   // Vibration

using Microsoft.Maui.Devices;

using NasreddinsSecretListener.Companion.Services;

namespace NasreddinsSecretListener.Companion.Services;

public class BleClientService : IBleClient
{
    private readonly ISettingsService _settings;
    public static readonly Guid ServiceUuid = Guid.Parse("6E400001-B5A3-F393-E0A9-E50E24DCCA9E");
    public static readonly Guid NotifyUuid = Guid.Parse("6E400003-B5A3-F393-E0A9-E50E24DCCA9E");
    private readonly HashSet<string> _probed = new();

    private readonly IBluetoothLE _ble;
    private readonly IAdapter _adapter;
    private readonly IFeedbackService _feedback;
    private IDevice? _connected;
    private ICharacteristic? _notifyChar;

    public event Action<NslDevice>? DeviceDiscovered;

    public event Action<string>? StateChanged;

    public BleClientService(ISettingsService settings, IFeedbackService feedback)
    {
        _settings = settings;
        _ble = CrossBluetoothLE.Current;
        _adapter = CrossBluetoothLE.Current.Adapter;
        _adapter.DeviceDiscovered += OnDeviceDiscovered;
        _adapter.ScanTimeout = 15000;
        _feedback = feedback;
    }

    private async Task<bool> ProbeHasNslServiceAsync(IDevice dev, CancellationToken ct)
    {
        try
        {
            if (!_adapter.ConnectedDevices.Contains(dev))
                await _adapter.ConnectToDeviceAsync(dev, new ConnectParameters(autoConnect: false, forceBleTransport: true), ct);

            var svc = await dev.GetServiceAsync(ServiceUuid);
            return svc != null;
        }
        catch { return false; }
        finally
        {
            try { if (_adapter.ConnectedDevices.Contains(dev)) await _adapter.DisconnectDeviceAsync(dev); } catch { }
        }
    }

    public async Task StartScanAsync()
    {
#if ANDROID
        StateChanged?.Invoke("Scan startet…");

        // Laufzeit-Berechtigungen anfragen (dein bestehender Helper)
        await PermissionHelper.EnsureBleScanAsync();

        // Nachprüfen – unterschiedliche Anforderungen je API-Level
        try
        {
            if (OperatingSystem.IsAndroidVersionAtLeast(31))
            {
                // Android 12+ : BLUETOOTH_* Runtime-Permissions
                var ctx = Android.App.Application.Context;
                bool granted =
                    ctx?.CheckSelfPermission(Android.Manifest.Permission.BluetoothScan) == Android.Content.PM.Permission.Granted &&
                    ctx?.CheckSelfPermission(Android.Manifest.Permission.BluetoothConnect) == Android.Content.PM.Permission.Granted;

                if (!granted)
                {
                    StateChanged?.Invoke("Bluetooth-Berechtigungen fehlen (Scan/Connect).");
                    return;
                }
            }
            else
            {
                // < Android 12 : Location genügt für BLE-Scan
                var status = await Microsoft.Maui.ApplicationModel.Permissions
                                  .CheckStatusAsync<Microsoft.Maui.ApplicationModel.Permissions.LocationWhenInUse>();
                if (status != Microsoft.Maui.ApplicationModel.PermissionStatus.Granted)
                {
                    StateChanged?.Invoke("Standort-Berechtigung fehlt (für BLE-Scan unter Android < 12).");
                    return;
                }
            }
        }
        catch (Exception ex)
        {
            StateChanged?.Invoke($"Berechtigungsprüfung fehlgeschlagen: {ex.Message}");
            return;
        }

        // Scanner starten (laufenden Scan vorher sauber stoppen)
        try
        {
            if (_adapter == null)
            {
                StateChanged?.Invoke("BLE-Adapter nicht verfügbar.");
                return;
            }

            if (_adapter.IsScanning)
                await _adapter.StopScanningForDevicesAsync();

            await _adapter.StartScanningForDevicesAsync();
            StateChanged?.Invoke("Scan aktiv.");
        }
        catch (Exception ex)
        {
            StateChanged?.Invoke($"Scan-Start fehlgeschlagen: {ex.Message}");
        }
#else
    StateChanged?.Invoke("BLE-Scan ist auf dieser Plattform nicht verfügbar.");
#endif
    }

    public async Task StopScanAsync()
    {
        await _adapter.StopScanningForDevicesAsync();
        StateChanged?.Invoke("Scan gestoppt.");
    }

    private void OnDeviceDiscovered(object? s, DeviceEventArgs e)
    {
        var name = string.IsNullOrWhiteSpace(e.Device.Name) ? "(unbenannt)" : e.Device.Name;
        var devVm = new NslDevice(e.Device.Id.ToString(), name, e.Device.Rssi);

        DeviceDiscovered?.Invoke(devVm);

        if (_probed.Add(devVm.Id)) // noch nicht geprüft
        {
            _ = Task.Run(async () =>
            {
                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(4));
                var hasSvc = await ProbeHasNslServiceAsync(e.Device, cts.Token);
                if (hasSvc)
                {
                    devVm.IsNsl = true;
                    DeviceDiscovered?.Invoke(devVm); // VM aktualisiert Eintrag
                }
            });
        }
    }

    public async Task<bool> ConnectByIdOrScanAsync(string deviceId, TimeSpan timeout)
    {
        // 1) Schnellversuch: KnownDevice
        try
        {
            var guid = Guid.Parse(deviceId);
            var known = await _adapter.ConnectToKnownDeviceAsync(guid);
            if (known != null)
                return await FinishConnectAndSubscribeAsync(known);
        }
        catch
        {
            // Ignorieren – nicht bekannt/erreichbar
        }

        // 2) Fallback: Scan bis genau dieses Gerät auftaucht
        var tcs = new TaskCompletionSource<IDevice?>(TaskCreationOptions.RunContinuationsAsynchronously);

        void Handler(object? s, DeviceEventArgs e)
        {
            if (e.Device.Id.ToString() == deviceId)
                tcs.TrySetResult(e.Device);
        }

        _adapter.DeviceDiscovered += Handler;
        try
        {
            StateChanged?.Invoke("Scanne nach 'Mein Gerät'…");
            await _adapter.StartScanningForDevicesAsync();

            var winnerTask = await Task.WhenAny(tcs.Task, Task.Delay(timeout));
            var winner = (winnerTask == tcs.Task) ? tcs.Task.Result : null;

            await _adapter.StopScanningForDevicesAsync();

            if (winner == null)
                return false;

            return await FinishConnectAndSubscribeAsync(winner);
        }
        catch (Exception ex)
        {
            StateChanged?.Invoke($"Scan/Connect-Fehler: {ex.Message}");
            return false;
        }
        finally
        {
            _adapter.DeviceDiscovered -= Handler;
        }
    }

    // Haptik: Überlappungen vermeiden + minimale Pause zwischen Mustern
    private readonly SemaphoreSlim _vibeGate = new(1, 1);

    private readonly TimeSpan _minGap = TimeSpan.FromMilliseconds(250);

    // ▼ NEU: Gemeinsamer Abschluss – verbinden + Notify abonnieren
    private async Task<bool> FinishConnectAndSubscribeAsync(IDevice dev)
    {
        try
        {
            await _adapter.ConnectToDeviceAsync(dev);
            _connected = dev;

            var service = await dev.GetServiceAsync(ServiceUuid);
            if (service is null) { StateChanged?.Invoke("NSL-Service nicht gefunden."); return false; }

            _notifyChar = await service.GetCharacteristicAsync(NotifyUuid);
            if (_notifyChar is null) { StateChanged?.Invoke("Notify-Char nicht gefunden."); return false; }

            _notifyChar.ValueUpdated += OnStatusUpdated;
            await _notifyChar.StartUpdatesAsync();
            StateChanged?.Invoke($"Verbunden: {dev.Name}");
            return true;
        }
        catch (Exception ex)
        {
            StateChanged?.Invoke($"Fehler: {ex.Message}");
            return false;
        }
    }

    private DateTime _lastVibe = DateTime.MinValue;
    private readonly TimeSpan _vibeCooldown = TimeSpan.FromMilliseconds(800); // minimaler Abstand

    // ▼ NEU: Sauber trennen
    public async Task DisconnectAsync()
    {
        try
        {
            if (_connected != null)
                await _adapter.DisconnectDeviceAsync(_connected);
        }
        catch { /* egal */ }
        finally
        {
            _connected = null;
            if (_notifyChar != null)
            {
                try
                {
                    _notifyChar.ValueUpdated -= OnStatusUpdated;
                    await _notifyChar.StopUpdatesAsync();
                }
                catch { }
                _notifyChar = null;
            }
        }
    }

    public async Task<bool> ConnectAndSubscribeAsync(string deviceId)
    {
        await StopScanAsync();
        IDevice? dev = null;
        try
        {
            var guid = Guid.Parse(deviceId);
            dev = await _adapter.ConnectToKnownDeviceAsync(guid);
        }
        catch
        {
            dev = _adapter.ConnectedDevices.FirstOrDefault(d => d.Id.ToString() == deviceId);
        }

        if (dev is null) { StateChanged?.Invoke("Gerät nicht gefunden."); return false; }

        try
        {
            if (!_adapter.ConnectedDevices.Contains(dev))
                await _adapter.ConnectToDeviceAsync(dev);

            StateChanged?.Invoke($"Verbunden: {dev.Name}");

            var service = await dev.GetServiceAsync(ServiceUuid);
            if (service is null) { StateChanged?.Invoke("Service nicht gefunden."); return false; }

            _notifyChar = await service.GetCharacteristicAsync(NotifyUuid);
            if (_notifyChar is null) { StateChanged?.Invoke("Notify-Char nicht gefunden."); return false; }

            _notifyChar.ValueUpdated += OnStatusUpdated;
            await _notifyChar.StartUpdatesAsync();
            StateChanged?.Invoke("Notify aktiviert.");
            return true;
        }
        catch (Exception ex)
        {
            StateChanged?.Invoke($"Fehler: {ex.Message}");
            return false;
        }
    }

    private void OnStatusUpdated(object? sender, CharacteristicUpdatedEventArgs e)
    {
        var data = e.Characteristic.Value;
        if (data is null || data.Length == 0) return;

        var val = data[0]; // 0x00=None, 0x01=Early, 0x02=Confirmed
        var text = val switch
        {
            0x00 => "Kein Magnet",
            0x01 => "Annäherung",
            0x02 => "Erkannt",
            _ => $"Unbekannter Status 0x{val:X2}"
        };

        // Haptik gemäß Vorgabe: 0x01 = 1x kurz, 0x02 = 2x kurz
        // Nicht blockierend ausführen (eigener Task)
        _ = _feedback.NotifyStatusAsync(val);

        // UI-Status aktualisieren
        MainThread.BeginInvokeOnMainThread(() =>
        {
            StateChanged?.Invoke($"Status: {text}");
        });
    }

    private async Task RequestPermissionsAsync()
    {
#if ANDROID
        var status = await Permissions.RequestAsync<Permissions.LocationWhenInUse>();
#endif
    }
}