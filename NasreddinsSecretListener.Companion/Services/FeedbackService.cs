// Services/FeedbackService.cs
namespace NasreddinsSecretListener.Companion.Services;

#if ANDROID

using Android.App;
using NasreddinsSecretListener.Companion.Platforms.Android;

#endif

public interface IFeedbackService
{
    /// <summary>
    ///     Zwei Haptik-Pulse hintereinander (Doppelpuls).
    /// </summary>
    Task HapticDoublePulseAsync();

    /// <summary>
    ///     Ein einfacher Haptik-Puls (ms aus SettingsService).
    /// </summary>
    void HapticPulse();

    /// <summary>
    ///     Statusabhängige Rückmeldung: 0x01 = Early (Annäherung) → 1× Puls, wenn aktiv 0x02 =
    ///     Confirmed (Magnet) → 1×/2× Puls je nach Setting
    /// </summary>
    Task NotifyStatusAsync(byte status);

    /// <summary>
    ///     Einfacher Ton (Stub für spätere Implementierung).
    /// </summary>
    void PlayBeep();
}

public sealed class FeedbackService : IFeedbackService
{
#if ANDROID

    private static void NotifyWatch(string title, string text, bool doublePulse)
    {
        try
        {
            var ctx = Application.Context;
            AndroidEventNotifier.ShowEvent(ctx, title, text, doublePulse);
        }
        catch { /* ignore */ }
    }

#endif

    public FeedbackService(ISettingsService settings)
    {
        _settings = settings;
    }

    public async Task HapticDoublePulseAsync()
    {
        var ms = Math.Max(1, _settings.HapticMs);
        try
        {
            Vibration.Default.Vibrate(TimeSpan.FromMilliseconds(ms));
            await Task.Delay(Math.Min(ms, 200));
            Vibration.Default.Vibrate(TimeSpan.FromMilliseconds(ms));
            _lastHaptic = DateTime.UtcNow;
        }
        catch
        {
            // still
        }
    }

    public void HapticPulse()
    {
        var now = DateTime.UtcNow;
        if (now - _lastHaptic < _minGap)
            return;

        var ms = Math.Max(1, _settings.HapticMs);
        try
        {
            Vibration.Default.Vibrate(TimeSpan.FromMilliseconds(ms));
            _lastHaptic = DateTime.UtcNow;
        }
        catch
        {
            // Gerät ohne Vibrationsmotor / DND / OEM-Blockaden → still ignorieren
        }
    }

    public async Task NotifyStatusAsync(byte status)
    {
        // 0x01 = Early (Annäherung)
        if (status == 0x01 && _settings.VibrateOnEarly)
        {
            HapticPulse();
#if ANDROID
            NotifyWatch("NSL (früh)", "Annäherung erkannt", doublePulse: false);
#endif
            // kleine Abklingzeit ähnlich wie vorher
            await Task.Delay(150);
            return;
        }

        // 0x02 = Confirmed (Magnet)
        if (status == 0x02 && _settings.VibrateOnConfirmed)
        {
            if (_settings.DoublePulseForConfirmed)
                await HapticDoublePulseAsync();
            else
                HapticPulse();
#if ANDROID
            NotifyWatch("NSL bestätigt", "Magnet erkannt", doublePulse: _settings.DoublePulseForConfirmed);
#endif
            await Task.Delay(150);
        }
    }

    public void PlayBeep()
    {
        // Platzhalter: hier später MediaElement/AudioTrack/Platform-Sound einhängen oder
        // systemweite Benachrichtigung über NotificationChannel abspielen. Vorläufig tun wir
        // nichts, damit keine OEM-Konflikte entstehen.
    }

    // Spam-Schutz wie bisher in BleClientService
    private readonly TimeSpan _minGap = TimeSpan.FromMilliseconds(250);

    private readonly ISettingsService _settings;
    private DateTime _lastHaptic = DateTime.MinValue;
}
