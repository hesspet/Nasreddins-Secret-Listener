using Android.App;
using Android.Content;
using Android.OS;

namespace NasreddinsSecretListener.Companion.Platforms.Android;

public static class NotificationHelper
{
    public const string ChannelEventsDouble = "nsl_events_double";
    public const string ChannelEventsShort = "nsl_events_short";

    public static void EnsureChannels(Context ctx)
    {
        if (Build.VERSION.SdkInt < BuildVersionCodes.O)
        {
            return;
        }

        var mgr = (NotificationManager)ctx.GetSystemService(Context.NotificationService)!;

        var chShort = new NotificationChannel(ChannelEventsShort, "NSL Events (kurz)", NotificationImportance.High);

        chShort.EnableVibration(true);
        chShort.SetVibrationPattern([0, 200]);
        chShort.EnableLights(false);
        chShort.SetSound(null, null);

        mgr.CreateNotificationChannel(chShort);

        var chDouble = new NotificationChannel(ChannelEventsDouble, "NSL Events (doppelt)", NotificationImportance.High);

        chDouble.EnableVibration(true);
        chDouble.SetVibrationPattern([0, 150, 100, 200]);
        chDouble.EnableLights(false);
        chDouble.SetSound(null, null);

        mgr.CreateNotificationChannel(chDouble);
    }
}
