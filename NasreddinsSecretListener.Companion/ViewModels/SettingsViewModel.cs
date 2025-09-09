// ViewModels/SettingsViewModel.cs
using CommunityToolkit.Mvvm.ComponentModel;
using NasreddinsSecretListener.Companion.Services;
using CommunityToolkit.Mvvm.Input;

namespace NasreddinsSecretListener.Companion.ViewModels;

public sealed partial class SettingsViewModel : ObservableObject
{
    public SettingsViewModel(ISettingsService settings, IBleClient ble)
    {
        _settings = settings;
        // Initial aus Service laden
        _vibrateOnEarly = _settings.VibrateOnEarly;
        _vibrateOnConfirmed = _settings.VibrateOnConfirmed;
        _doublePulseForConfirmed = _settings.DoublePulseForConfirmed;
        _autoConnectMyDevice = _settings.AutoConnectMyDevice;
        _showOnlyNsl = _settings.ShowOnlyNsl;
        _hapticMs = _settings.HapticMs;
        _ble = ble;
    }

    public bool AutoConnectMyDevice
    {
        get => _autoConnectMyDevice;
        set
        {
            if (SetProperty(ref _autoConnectMyDevice, value))
                _settings.AutoConnectMyDevice = value;
        }
    }

    public bool DoublePulseForConfirmed
    {
        get => _doublePulseForConfirmed;
        set
        {
            if (SetProperty(ref _doublePulseForConfirmed, value))
                _settings.DoublePulseForConfirmed = value;
        }
    }

    public int HapticMs
    {
        get => _hapticMs;
        set
        {
            if (value < 1)
                value = 1;
            if (SetProperty(ref _hapticMs, value))
                _settings.HapticMs = value;
        }
    }

    public bool ShowOnlyNsl
    {
        get => _showOnlyNsl;
        set
        {
            if (SetProperty(ref _showOnlyNsl, value))
                _settings.ShowOnlyNsl = value;
        }
    }

    public bool VibrateOnConfirmed
    {
        get => _vibrateOnConfirmed;
        set
        {
            if (SetProperty(ref _vibrateOnConfirmed, value))
                _settings.VibrateOnConfirmed = value;
        }
    }

    public bool VibrateOnEarly
    {
        get => _vibrateOnEarly;
        set
        {
            if (SetProperty(ref _vibrateOnEarly, value))
                _settings.VibrateOnEarly = value;
        }
    }

    [RelayCommand]
    public async Task ExitApp()
    {
        try
        {
            await _ble.DisconnectAsync();
        }
        catch { /* best effort */ }

#if ANDROID
        try
        {
            var ctx = Android.App.Application.Context;
            NasreddinsSecretListener.Companion.NslBleForegroundService.Stop(ctx);
        }
        catch { /* best effort */ }
#endif

        // ganz kurz Luft holen, damit der Service sauber stoppt
        await Task.Delay(100);

        // App schließen (MAUI .NET 8/9)
        Application.Current?.Quit();
    }

    private readonly ISettingsService _settings;
    private bool _autoConnectMyDevice;
    private bool _doublePulseForConfirmed;
    private int _hapticMs;
    private readonly IBleClient _ble;
    private bool _showOnlyNsl;
    private bool _vibrateOnConfirmed;
    private bool _vibrateOnEarly;
}
