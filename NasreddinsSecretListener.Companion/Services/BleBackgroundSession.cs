// Services/BleBackgroundSession.cs
using Plugin.BLE;
using Plugin.BLE.Abstractions;                 // <-- für DeviceState
using Plugin.BLE.Abstractions.Contracts;
using Plugin.BLE.Abstractions.EventArgs;

namespace NasreddinsSecretListener.Companion.Services;

public static class BleBackgroundSession
{
    public static async Task EnsureConnectedAsync(string deviceId, CancellationToken ct)
    {
        if (_adapter == null)
            throw new InvalidOperationException("Adapter not ready");

        if (IsConnected())
            return;

        IDevice? candidate = null;

        // Versuch 1: bekannte Guid direkt verbinden
        if (Guid.TryParse(deviceId, out var guid))
        {
            try
            {
                candidate = await _adapter
                    .ConnectToKnownDeviceAsync(guid, cancellationToken: ct)
                    .ContinueWith(t => t.IsCompletedSuccessfully ? t.Result : null, ct)
                    .ConfigureAwait(false);
            }
            catch
            {
                // Ignorieren; wir versuchen gleich den Scan-Fallback
                candidate = null;
            }
        }

        // Versuch 2: Scan + Match via Id/Name
        if (candidate == null)
        {
            IDevice? found = null;
            void Handler(object? s, DeviceEventArgs e)
            {
                if (e.Device == null) return;

                var idMatch = e.Device.Id.ToString().Equals(deviceId, StringComparison.OrdinalIgnoreCase);
                var nameMatch = !string.IsNullOrEmpty(e.Device.Name) &&
                                e.Device.Name.Equals(deviceId, StringComparison.OrdinalIgnoreCase);

                if (idMatch || nameMatch)
                    found = e.Device;
            }

            _adapter.ScanMode = ScanMode.Balanced;
            _adapter.DeviceDiscovered += Handler;

            try
            {
                await _adapter.StartScanningForDevicesAsync(cancellationToken: ct).ConfigureAwait(false);
                // kurze Scan-Phase
                await Task.Delay(2000, ct).ConfigureAwait(false);
            }
            catch (OperationCanceledException) { }
            finally
            {
                try { await _adapter.StopScanningForDevicesAsync().ConfigureAwait(false); } catch { /* ignore */ }
                _adapter.DeviceDiscovered -= Handler;
            }

            if (found != null)
                candidate = found;
        }

        // Verbinden (falls ein Kandidat gefunden wurde)
        if (candidate != null)
        {
            await _adapter.ConnectToDeviceAsync(candidate, cancellationToken: ct).ConfigureAwait(false);
            _device = candidate;

            // doppelte Registrierung vermeiden
            _adapter.DeviceConnectionLost -= Adapter_DeviceConnectionLost;
            _adapter.DeviceDisconnected -= Adapter_DeviceConnectionLost;

            _adapter.DeviceConnectionLost += Adapter_DeviceConnectionLost;
            _adapter.DeviceDisconnected += Adapter_DeviceConnectionLost;

            // TODO: Ab hier Services auflösen, Characteristics finden, Notifications abonnieren
        }
    }

    public static void SetTargetDevice(string idOrName)
    {
        Preferences.Set(PrefDeviceId, idOrName ?? string.Empty);
    }

    public static async Task StartAsync(CancellationToken ct)
    {
        _adapter ??= CrossBluetoothLE.Current.Adapter;

        // Hauptloop: solange laufen, wie nicht abgebrochen
        while (!ct.IsCancellationRequested)
        {
            try
            {
                if (!IsConnected())
                {
                    var targetId = Preferences.Get(PrefDeviceId, string.Empty);
                    if (!string.IsNullOrWhiteSpace(targetId))
                    {
                        await EnsureConnectedAsync(targetId, ct).ConfigureAwait(false);
                    }
                }

                // TODO: Hier ggf. Notification-Subscriptions prüfen/erneuern

                await Task.Delay(TimeSpan.FromSeconds(3), ct).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                // normal beim Stop
                break;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("BLE loop error: " + ex);
                await Task.Delay(TimeSpan.FromSeconds(5), ct).ConfigureAwait(false);
            }
        }
    }

    public static void Stop()
    {
        // Wenn du einen eigenen CTS verwendest, hier Cancel() aufrufen.
        // In der aktuellen Architektur kommt das CancellationToken vom Foreground Service.
        // Diese Methode bleibt für API-Kompatibilität bestehen.
    }

    private const string PrefDeviceId = "BleDeviceId";
    private static IAdapter? _adapter;
    private static IDevice? _device;

    // alternativ ohne using:
    // => _device != null && _device.State == Plugin.BLE.Abstractions.DeviceState.Connected;
    private static void Adapter_DeviceConnectionLost(object? sender, DeviceEventArgs e)
    {
        if (_device != null && e.Device?.Id == _device.Id)
        {
            _device = null;
        }
    }

    // Ziel-Geräte-ID (Guid als string oder Name/MAC je nach Plattform)
    private static bool IsConnected()
        => _device != null && _device.State == DeviceState.Connected;
}