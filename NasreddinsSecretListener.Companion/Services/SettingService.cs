// Services/SettingsService.cs
using Microsoft.Maui.Storage;

namespace NasreddinsSecretListener.Companion.Services;

public interface ISettingsService
{
    bool AutoConnectMyDevice { get; set; }
    bool DoublePulseForConfirmed { get; set; }
    int HapticMs { get; set; }
    bool ShowOnlyNsl { get; set; }
    bool VibrateOnConfirmed { get; set; }
    bool VibrateOnEarly { get; set; }
}

public sealed class SettingsService : ISettingsService
{
    public bool AutoConnectMyDevice
    {
        get => Preferences.Get(Key_AutoConnectMyDevice, Default_AutoConnectMyDevice);
        set => Preferences.Set(Key_AutoConnectMyDevice, value);
    }

    public bool DoublePulseForConfirmed
    {
        get => Preferences.Get(Key_DoublePulseConfirmed, Default_DoublePulseConfirmed);
        set => Preferences.Set(Key_DoublePulseConfirmed, value);
    }

    public int HapticMs
    {
        get => Preferences.Get(Key_HapticMs, Default_HapticMs);
        set => Preferences.Set(Key_HapticMs, value);
    }

    public bool ShowOnlyNsl
    {
        get => Preferences.Get(Key_ShowOnlyNsl, Default_ShowOnlyNsl);
        set => Preferences.Set(Key_ShowOnlyNsl, value);
    }

    public bool VibrateOnConfirmed
    {
        get => Preferences.Get(Key_VibrateOnConfirmed, Default_VibrateOnConfirmed);
        set => Preferences.Set(Key_VibrateOnConfirmed, value);
    }

    public bool VibrateOnEarly
    {
        get => Preferences.Get(Key_VibrateOnEarly, Default_VibrateOnEarly);
        set => Preferences.Set(Key_VibrateOnEarly, value);
    }

    private const bool Default_AutoConnectMyDevice = false;

    private const bool Default_DoublePulseConfirmed = false;

    private const int Default_HapticMs = 150;

    private const bool Default_ShowOnlyNsl = false;

    private const bool Default_VibrateOnConfirmed = true;

    // Defaultwerte
    private const bool Default_VibrateOnEarly = true;

    private const string Key_AutoConnectMyDevice = "settings.autoconnect";

    private const string Key_DoublePulseConfirmed = "settings.vibrate.doublepulse";

    private const string Key_HapticMs = "settings.haptic.ms";

    private const string Key_ShowOnlyNsl = "settings.onlynsl";

    private const string Key_VibrateOnConfirmed = "settings.vibrate.confirmed";

    // zentrale Keys
    private const string Key_VibrateOnEarly = "settings.vibrate.early";
}