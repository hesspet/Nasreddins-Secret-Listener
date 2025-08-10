using NasreddinsSecretListener.Companion.Helper;
using NasreddinsSecretListener.Companion.Models;
using Plugin.BLE;
using Plugin.BLE.Abstractions;
using Plugin.BLE.Abstractions.Contracts;
using Plugin.BLE.Abstractions.EventArgs;

namespace NasreddinsSecretListener.Companion.Services;

public class BleClientService : IBleClient
{
    public static readonly Guid ServiceUuid = Guid.Parse("6E400001-B5A3-F393-E0A9-E50E24DCCA9E");
    public static readonly Guid NotifyUuid = Guid.Parse("6E400003-B5A3-F393-E0A9-E50E24DCCA9E");
    private readonly HashSet<string> _probed = new();

    private readonly IBluetoothLE _ble;
    private readonly IAdapter _adapter;

    private IDevice? _connected;
    private ICharacteristic? _notifyChar;

    public event Action<NslDevice>? DeviceDiscovered;

    public event Action<string>? StateChanged;

    public BleClientService()
    {
        _ble = CrossBluetoothLE.Current;
        _adapter = CrossBluetoothLE.Current.Adapter;
        _adapter.DeviceDiscovered += OnDeviceDiscovered;
        _adapter.ScanTimeout = 15000;
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
        StateChanged?.Invoke("Scan startet...");
        await PermissionHelper.EnsureBleScanAsync();
        await _adapter.StartScanningForDevicesAsync();
        StateChanged?.Invoke("Scan aktiv.");
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
        var val = data[0];
        var text = val switch
        {
            0x00 => "Kein Magnet",
            0x01 => "Annäherung",
            0x02 => "Erkannt",
            _ => $"Unbekannter Status 0x{val:X2}"
        };

        MainThread.BeginInvokeOnMainThread(() =>
        {
            StateChanged?.Invoke($"Status: {text}");
            try
            {
                var dur = val == 0x02 ? 300 : 120;
                Vibration.Default.Vibrate(TimeSpan.FromMilliseconds(dur));
            }
            catch { }
        });
    }

    private async Task RequestPermissionsAsync()
    {
#if ANDROID
        var status = await Permissions.RequestAsync<Permissions.LocationWhenInUse>();
#endif
    }
}