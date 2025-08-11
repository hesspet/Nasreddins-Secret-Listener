using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Maui.Storage;

namespace NasreddinsSecretListener.Companion.ViewModels;

public sealed class SettingsViewModel : ObservableObject
{
    public bool AutoConnectMyDevice
    {
        get => _autoConnectMyDevice;
        set
        {
            if (SetProperty(ref _autoConnectMyDevice, value))
                Preferences.Set(Key_AutoConnectMyDevice, value);
        }
    }

    public bool DoublePulseForConfirmed
    {
        get => _doublePulseForConfirmed;
        set
        {
            if (SetProperty(ref _doublePulseForConfirmed, value))
                Preferences.Set(Key_DoublePulseConfirmed, value);
        }
    }

    public int HapticMs
    {
        get => _hapticMs;
        set
        {
            if (SetProperty(ref _hapticMs, value))
                Preferences.Set(Key_HapticMs, value);
        }
    }

    public bool ShowOnlyNsl
    {
        get => _showOnlyNsl;
        set
        {
            if (SetProperty(ref _showOnlyNsl, value))
                Preferences.Set(Key_ShowOnlyNsl, value);
        }
    }

    public bool VibrateOnConfirmed
    {
        get => _vibrateOnConfirmed;
        set
        {
            if (SetProperty(ref _vibrateOnConfirmed, value))
                Preferences.Set(Key_VibrateOnConfirmed, value);
        }
    }

    // --- Öffentliche Properties mit Persistenz in Preferences
    public bool VibrateOnEarly
    {
        get => _vibrateOnEarly;
        set
        {
            if (SetProperty(ref _vibrateOnEarly, value))
                Preferences.Set(Key_VibrateOnEarly, value);
        }
    }

    // --- Initial-Laden (ruft z.B. die Page in OnAppearing auf)
    public void Load()
    {
        VibrateOnEarly = Preferences.Get(Key_VibrateOnEarly, true);
        VibrateOnConfirmed = Preferences.Get(Key_VibrateOnConfirmed, true);
        DoublePulseForConfirmed = Preferences.Get(Key_DoublePulseConfirmed, false);
        AutoConnectMyDevice = Preferences.Get(Key_AutoConnectMyDevice, false);
        ShowOnlyNsl = Preferences.Get(Key_ShowOnlyNsl, false);
        HapticMs = Preferences.Get(Key_HapticMs, 150);
    }

    private const string Key_AutoConnectMyDevice = "settings.autoconnect";

    private const string Key_DoublePulseConfirmed = "settings.vibrate.doublepulse";

    private const string Key_HapticMs = "settings.haptic.ms";

    private const string Key_ShowOnlyNsl = "settings.onlynsl";

    private const string Key_VibrateOnConfirmed = "settings.vibrate.confirmed";

    // Preference Keys
    private const string Key_VibrateOnEarly = "settings.vibrate.early";

    private bool _autoConnectMyDevice;

    private bool _doublePulseForConfirmed;

    private int _hapticMs;

    private bool _showOnlyNsl;

    private bool _vibrateOnConfirmed;

    // --- Backing fields
    private bool _vibrateOnEarly;
}