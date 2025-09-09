using Android.App;
using Android.Content;
using Android.OS;

namespace NasreddinsSecretListener.Companion.Platforms.Android;

public static class NotificationHelper
{
    public const string ChannelEventsDouble = "nsl_events_double";
    public const string ChannelEventsShort = "nsl_events_short";
    public const string ChannelForeground = "nsl_foreground";  

    public static void EnsureChannels(Context ctx)
    {
        if (Build.VERSION.SdkInt < BuildVersionCodes.O)
            return;

        var mgr = (NotificationManager)ctx.GetSystemService(Context.NotificationService)!;

        // Event: kurz
        var chShort = new NotificationChannel(
            ChannelEventsShort, "NSL Events (kurz)", NotificationImportance.High);
        chShort.EnableVibration(true);
        chShort.SetVibrationPattern(new long[] { 0, 200 });
        chShort.EnableLights(false);
        chShort.SetSound(null, null);
        mgr.CreateNotificationChannel(chShort);

        // Event: doppelt
        var chDouble = new NotificationChannel(
            ChannelEventsDouble, "NSL Events (doppelt)", NotificationImportance.High);
        chDouble.EnableVibration(true);
        chDouble.SetVibrationPattern(new long[] { 0, 150, 100, 200 });
        chDouble.EnableLights(false);
        chDouble.SetSound(null, null);
        mgr.CreateNotificationChannel(chDouble);
    }

    public static void EnsureForegroundChannel(Context ctx)
    {
        if (Build.VERSION.SdkInt < BuildVersionCodes.O)
            return;

        var mgr = (NotificationManager)ctx.GetSystemService(Context.NotificationService)!;
        var ch = new NotificationChannel(
            ChannelForeground, "NSL Hintergrunddienst", NotificationImportance.Low);
        ch.EnableVibration(false);
        ch.SetSound(null, null);
        ch.LockscreenVisibility = NotificationVisibility.Secret;
        mgr.CreateNotificationChannel(ch);
    }
}
