// ViewModels/SettingsViewModel.cs
using CommunityToolkit.Mvvm.ComponentModel;
using NasreddinsSecretListener.Companion.Services;

namespace NasreddinsSecretListener.Companion.ViewModels;

public sealed class SettingsViewModel : ObservableObject
{
    public SettingsViewModel(ISettingsService settings)
    {
        _settings = settings;
        // Initial aus Service laden
        _vibrateOnEarly = _settings.VibrateOnEarly;
        _vibrateOnConfirmed = _settings.VibrateOnConfirmed;
        _doublePulseForConfirmed = _settings.DoublePulseForConfirmed;
        _autoConnectMyDevice = _settings.AutoConnectMyDevice;
        _showOnlyNsl = _settings.ShowOnlyNsl;
        _hapticMs = _settings.HapticMs;
    }

    public bool AutoConnectMyDevice
    {
        get => _autoConnectMyDevice;
        set { if (SetProperty(ref _autoConnectMyDevice, value)) _settings.AutoConnectMyDevice = value; }
    }

    public bool DoublePulseForConfirmed
    {
        get => _doublePulseForConfirmed;
        set { if (SetProperty(ref _doublePulseForConfirmed, value)) _settings.DoublePulseForConfirmed = value; }
    }

    public int HapticMs
    {
        get => _hapticMs;
        set
        {
            if (value < 1) value = 1;
            if (SetProperty(ref _hapticMs, value))
                _settings.HapticMs = value;
        }
    }

    public bool ShowOnlyNsl
    {
        get => _showOnlyNsl;
        set { if (SetProperty(ref _showOnlyNsl, value)) _settings.ShowOnlyNsl = value; }
    }

    public bool VibrateOnConfirmed
    {
        get => _vibrateOnConfirmed;
        set { if (SetProperty(ref _vibrateOnConfirmed, value)) _settings.VibrateOnConfirmed = value; }
    }

    public bool VibrateOnEarly
    {
        get => _vibrateOnEarly;
        set { if (SetProperty(ref _vibrateOnEarly, value)) _settings.VibrateOnEarly = value; }
    }

    private readonly ISettingsService _settings;
    private bool _autoConnectMyDevice;
    private bool _doublePulseForConfirmed;
    private int _hapticMs;
    private bool _showOnlyNsl;
    private bool _vibrateOnConfirmed;
    private bool _vibrateOnEarly;
}