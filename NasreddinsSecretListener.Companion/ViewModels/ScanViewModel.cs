using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NasreddinsSecretListener.Companion.Models;
using NasreddinsSecretListener.Companion.Services;
using Microsoft.Maui.Storage;

namespace NasreddinsSecretListener.Companion.ViewModels;

public partial class ScanViewModel : ObservableObject
{
    public ScanViewModel(IBleClient ble)
    {
        _ble = ble;
        _ble.DeviceDiscovered += Ble_DeviceDiscovered;
        _ble.StateChanged += s => MainThread.BeginInvokeOnMainThread(() => StatusText = s);

        StartScanCommand = new AsyncRelayCommand(StartScanAsync);
        StopScanCommand = new AsyncRelayCommand(StopScanAsync);
        ConnectAndListenCommand = new AsyncRelayCommand(ConnectAndListenAsync);
        _ = LoadMyDeviceAsync();
    }

    // Public Command fürs Claiming:
    public IAsyncRelayCommand ClaimSelectedCommand => new AsyncRelayCommand(async () =>
    {
        if (SelectedDevice is null) return;
        await SaveMyDeviceAsync(SelectedDevice.Id);
        foreach (var d in Devices) d.IsMine = (d.Id == SelectedDevice.Id);
    });

    public IAsyncRelayCommand ConnectAndListenCommand { get; }
    public IAsyncRelayCommand ConnectMyDeviceCommand => new AsyncRelayCommand(ConnectMyAsync);
    public ObservableCollection<NslDevice> Devices { get; } = new();
    public IAsyncRelayCommand StartScanCommand { get; }
    public IAsyncRelayCommand StopScanCommand { get; }

    public async Task ClaimSelectedAsync()
    {
        if (SelectedDevice is null) { StatusText = "Bitte Gerät wählen."; return; }
        await SaveMyDeviceAsync(SelectedDevice.Id);
        foreach (var d in Devices) d.IsMine = (d.Id == SelectedDevice.Id);
        ResortDevices();
        StatusText = $"„{SelectedDevice.Name}“ als *Mein Gerät* gespeichert.";
        HasMyDevice = true; // redundant, aber explizit
    }

    public async Task ConnectAndListenAsync()
    {
        if (SelectedDevice is null) { StatusText = "Bitte Gerät wählen."; return; }
        StatusText = $"Verbinde zu {SelectedDevice.Name} ...";
        var ok = await _ble.ConnectAndSubscribeAsync(SelectedDevice.Id);
        IsConnected = ok;
        StatusText = ok ? "Verbunden. Lausche auf Status..." : "Verbindung fehlgeschlagen.";

        if (ok)
        {
            MainThread.BeginInvokeOnMainThread(async () =>
            {
                try { await Shell.Current.GoToAsync("//status"); } catch { /* ignore */ }
            });
        }
    }

    public async Task LoadMyDeviceAsync()
    {
        _myDeviceId = await SecureStorage.GetAsync(MyDeviceKey);
        HasMyDevice = !string.IsNullOrEmpty(_myDeviceId); // <-- statt hasMyDevice
    }

    public async Task StartScanAsync()
    {
        Devices.Clear();
        StatusText = "Scannen...";
        await _ble.StartScanAsync();
    }

    public async Task StopScanAsync()
    {
        await _ble.StopScanAsync();
        StatusText = "Scan gestoppt.";
        // optional: IsConnected = false; wenn Du hier trennst
    }

    public async Task TryAutoConnectAsync()
    {
        // Lade gespeichertes "Mein Gerät"
        await LoadMyDeviceAsync();

        // Nur wenn Einstellung aktiv ist, ein Gerät gespeichert ist,
        // noch keine Verbindung besteht und wir es in diesem Lebenszyklus
        // noch nicht versucht haben.
        var wantAuto = Preferences.Get(AutoConnectPrefKey, false);
        if (!wantAuto || string.IsNullOrEmpty(_myDeviceId) || IsConnected || _autoConnectTried)
            return;

        _autoConnectTried = true;

        StatusText = "Auto-Connect zu 'Mein Gerät'…";
        var ok = await _ble.ConnectByIdOrScanAsync(_myDeviceId, TimeSpan.FromSeconds(12));
        IsConnected = ok;
        StatusText = ok ? "Verbunden. Lausche auf Status…" : "Auto-Connect fehlgeschlagen. Bitte näher ran und erneut versuchen.";

        if (ok)
        {
            MainThread.BeginInvokeOnMainThread(async () =>
            {
                try { await Shell.Current.GoToAsync("//status"); } catch { /* ignore */ }
            });
        }
    }

    private const string AutoConnectPrefKey = "settings.autoconnect";
    private const string MyDeviceKey = "nsl.myDeviceId";
    private readonly IBleClient _ble;
    private bool _autoConnectTried; // damit wir es pro Anzeigen der Seite nur einmal versuchen
    private string? _myDeviceId;
    [ObservableProperty] private bool hasMyDevice;
    [ObservableProperty] private bool isConnected;

    // für Button-IsEnabled
    [ObservableProperty]
    private NslDevice? selectedDevice;

    [ObservableProperty]
    private string statusText = "Bereit.";

    private void Ble_DeviceDiscovered(NslDevice dev)
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            var existing = Devices.FirstOrDefault(d => d.Id == dev.Id);
            if (existing is null)
            {
                // Neu einsortieren (optional sortiert einfügen)
                Devices.Add(dev);
                existing = dev;
            }
            else
            {
                // Werte aktualisieren (mutable Properties)
                existing.Rssi = dev.Rssi;
                existing.IsNsl = existing.IsNsl || dev.IsNsl;
                existing.ShortId ??= dev.ShortId;
                existing.Status ??= dev.Status;
                existing.Mac ??= dev.Mac;
            }

            // „Mein Gerät“ hervorheben
            if (_myDeviceId != null && existing.Id == _myDeviceId)
                existing.IsMine = true;

            // (Optional) Resortieren nach IsMine/IsNsl/RSSI:
            ResortDevices();
        });
    }

    private async Task ConnectMyAsync()
    {
        if (string.IsNullOrEmpty(_myDeviceId))
        {
            StatusText = "Kein 'Mein Gerät' gespeichert.";
            return;
        }

        StatusText = "Verbinde zu 'Mein Gerät'…";
        var ok = await _ble.ConnectByIdOrScanAsync(_myDeviceId, TimeSpan.FromSeconds(12));
        IsConnected = ok;                                // <--
        StatusText = ok ? "Verbunden. Lausche auf Status…" : "Nicht gefunden. Bitte näher ran und erneut versuchen.";

        if (ok)
        {
            MainThread.BeginInvokeOnMainThread(async () =>
            {
                try { await Shell.Current.GoToAsync("//status"); } catch { /* ignore */ }
            });
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
            foreach (var d in ordered) Devices.Add(d);
        }
    }

    private async Task SaveMyDeviceAsync(string id)
    {
        _myDeviceId = id;
        HasMyDevice = true; // <-- statt hasMyDevice
        await SecureStorage.SetAsync(MyDeviceKey, id);
    }
}