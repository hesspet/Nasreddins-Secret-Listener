using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NasreddinsSecretListener.Companion.Models;
using NasreddinsSecretListener.Companion.Services;
using Microsoft.Maui.Storage;
using System.Threading.Tasks;

namespace NasreddinsSecretListener.Companion.ViewModels;

public sealed partial class ScanViewModel : ObservableObject, IDisposable
{
    public ScanViewModel(IBleClient ble, ISettingsService settings, ILogService log)
    {
        _log = log;
        _ble = ble;
        _settings = settings; // <— wichtig: speichern!

        _ble.DeviceDiscovered += Ble_DeviceDiscovered;
        _ble.StateChanged += s => Microsoft.Maui.ApplicationModel.MainThread
             .BeginInvokeOnMainThread(async () =>
             {
                 // Status immer anzeigen
                 StatusText = s ?? string.Empty;

                 // Grobe Heuristik: "Verbunden" => als connected werten
                 var isConnectedNow = s?.StartsWith("Verbunden", StringComparison.OrdinalIgnoreCase) == true;

                 if (isConnectedNow)
                 {
                     IsConnected = true;

                     // Automatische Navigation nur, wenn Auto-Connect gewünscht, wir noch nicht
                     // navigiert haben, und eine Shell verfügbar ist.
                     if (_settings.AutoConnectMyDevice && !_navigatedOnAutoConnect && Shell.Current is not null)
                     {
                         _navigatedOnAutoConnect = true;
                         try
                         {
                             await Shell.Current.GoToAsync("//status");
                         }
                         catch { /* Navigation ist best-effort */ }
                     }
                 }
                 else if (s?.Contains("Getrennt", StringComparison.OrdinalIgnoreCase) == true
                       || s?.Contains("nicht verbunden", StringComparison.OrdinalIgnoreCase) == true)
                 {
                     // Bei Disconnect den Guard zurücksetzen
                     IsConnected = false;
                     _navigatedOnAutoConnect = false;
                 }
             });

        // gespeichertes Gerät laden (fire & forget ist hier okay)
        _ = LoadMyDeviceAsync();

        _navigatedOnAutoConnect = false;
    }

    public ObservableCollection<NslDevice> Devices { get; } = new();

    public void Dispose()
    {
        _ble.DeviceDiscovered -= Ble_DeviceDiscovered;
        // _ble.StateChanged via Lambda: bei Bedarf eine explizite Unsubscribe-API im IBleClient vorsehen
    }

    // ===== Internes =====
    private const string MyDeviceKey = "nsl.myDeviceId";

    private readonly IBleClient _ble;
    private readonly ILogService _log;
    private readonly ISettingsService _settings;
    private bool _autoConnectTried;

    // pro „Sicht“ nur einmal versuchen
    private string? _myDeviceId;

    private bool _navigatedOnAutoConnect;

    // ===== Properties =====
    [ObservableProperty] private bool hasMyDevice;

    [ObservableProperty] private bool isConnected;
    [ObservableProperty] private NslDevice? selectedDevice;
    [ObservableProperty] private string statusText = "Bereit.";

    private void Ble_DeviceDiscovered(NslDevice dev)
    {
        Microsoft.Maui.ApplicationModel.MainThread.BeginInvokeOnMainThread(() =>
        {
            // 🔎 Filter nur NSL-Geräte, wenn Setting aktiv ist
            if (_settings.ShowOnlyNsl && !dev.IsNsl)
                return;

            var existing = Devices.FirstOrDefault(d => d.Id == dev.Id);
            if (existing is null)
            {
                Devices.Add(dev);
                existing = dev;
            }
            else
            {
                existing.Rssi = dev.Rssi;
                existing.IsNsl = existing.IsNsl || dev.IsNsl;
                existing.ShortId ??= dev.ShortId;
                existing.Status ??= dev.Status;
                existing.Mac ??= dev.Mac;
            }

            if (_myDeviceId != null && existing.Id == _myDeviceId)
                existing.IsMine = true;

            ResortDevices();
        });
    }

    [RelayCommand]
    private async Task ClaimSelected()
    {
        if (SelectedDevice is null)
        {
            StatusText = "Bitte Gerät wählen.";
            return;
        }

        await SaveMyDeviceAsync(SelectedDevice.Id);
        foreach (var d in Devices)
            d.IsMine = (d.Id == SelectedDevice.Id);
        ResortDevices();
        StatusText = $"„{SelectedDevice.Name}“ als *Mein Gerät* gespeichert.";
        HasMyDevice = true;
    }

    [RelayCommand]
    private async Task ConnectAndListen()
    {
        if (SelectedDevice is null)
        {
            StatusText = "Bitte Gerät wählen.";
            return;
        }

        StatusText = $"Verbinde zu {SelectedDevice.Name} …";
        var ok = await _ble.ConnectAndSubscribeAsync(SelectedDevice.Id);
        IsConnected = ok;
        StatusText = ok ? "Verbunden. Lausche auf Status…" : "Verbindung fehlgeschlagen.";

        if (ok)
        {
            await Microsoft.Maui.ApplicationModel.MainThread.InvokeOnMainThreadAsync(
                () => Shell.Current.GoToAsync("//status")
            );
        }
    }

    [RelayCommand]
    private async Task ConnectMyDevice()
    {
        if (string.IsNullOrEmpty(_myDeviceId))
        {
            StatusText = "Kein 'Mein Gerät' gespeichert.";
            return;
        }

        StatusText = "Verbinde zu 'Mein Gerät'…";
        var ok = await _ble.ConnectByIdOrScanAsync(_myDeviceId, TimeSpan.FromSeconds(12));
        IsConnected = ok;
        StatusText = ok ? "Verbunden. Lausche auf Status…" : "Nicht gefunden. Bitte näher ran und erneut versuchen.";

        if (ok)
        {
            await Microsoft.Maui.ApplicationModel.MainThread.InvokeOnMainThreadAsync(
                () => Shell.Current.GoToAsync("//status")
            );
        }
    }

    private async Task LoadMyDeviceAsync()
    {
        try
        {
            _myDeviceId = await SecureStorage.GetAsync(MyDeviceKey);
            HasMyDevice = !string.IsNullOrEmpty(_myDeviceId);
        }
        catch
        {
            _myDeviceId = null;
            HasMyDevice = false;
        }
    }

    private void ResortDevices()
    {
        var ordered = Devices
            .OrderByDescending(d => d.IsMine)
            .ThenByDescending(d => d.IsNsl)
            .ThenByDescending(d => d.Rssi)
            .ToList();

        if (!ordered.SequenceEqual(Devices))
        {
            Devices.Clear();
            foreach (var d in ordered)
                Devices.Add(d);
        }
    }

    private async Task SaveMyDeviceAsync(string id)
    {
        _myDeviceId = id;
        HasMyDevice = true;
        try
        {
            await SecureStorage.SetAsync(MyDeviceKey, id);
        }
        catch { /* ignorieren, UI-Status bleibt gesetzt */ }
    }

    [RelayCommand]
    private async Task StartScan()
    {
        Devices.Clear();
        StatusText = "Scannen…";
        await _ble.StartScanAsync();
    }

    [RelayCommand]
    private async Task StopScan()
    {
        await _ble.StopScanAsync();
        StatusText = "Scan gestoppt.";
    }

    // Kannst du z.B. im OnAppearing der Page aufrufen
    [RelayCommand]
    private async Task TryAutoConnect()
    {
        await LoadMyDeviceAsync();

        // ✅ Kein Magic-String: zentral aus SettingsService
        var wantAuto = _settings.AutoConnectMyDevice;
        if (!wantAuto || string.IsNullOrEmpty(_myDeviceId) || IsConnected || _autoConnectTried)
            return;

        _autoConnectTried = true;

        StatusText = "Auto-Connect zu 'Mein Gerät'…";
        var ok = await _ble.ConnectByIdOrScanAsync(_myDeviceId, TimeSpan.FromSeconds(12));
        IsConnected = ok;
        StatusText = ok ? "Verbunden. Lausche auf Status…" : "Auto-Connect fehlgeschlagen. Bitte näher ran und erneut versuchen.";

        if (ok)
        {
            await Microsoft.Maui.ApplicationModel.MainThread.InvokeOnMainThreadAsync(
                () => Shell.Current.GoToAsync("//status")
            );
        }
    }
}
