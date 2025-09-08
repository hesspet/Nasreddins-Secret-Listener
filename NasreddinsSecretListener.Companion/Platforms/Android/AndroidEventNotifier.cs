using Android.Content;
using AndroidX.Core.App;
using Android.App;
using Android.OS;
using AndroidRes = NasreddinsSecretListener.Companion.Resource;
using static NasreddinsSecretListener.Companion.Platforms.Android.NotificationHelper;

namespace NasreddinsSecretListener.Companion.Platforms.Android;

public static class AndroidEventNotifier
{
    public static void RequestPostNotificationsIfNeeded(Activity activity)
    {
        if (Build.VERSION.SdkInt >= BuildVersionCodes.Tiramisu)
        {
            var mgr = NotificationManagerCompat.From(activity);
            if (!mgr.AreNotificationsEnabled())
            {
                ActivityCompat.RequestPermissions(
                    activity, new string[] { global::Android.Manifest.Permission.PostNotifications }, requestCode: 2001);
            }
        }
    }

    public static void ShowEvent(Context ctx, string title, string text, bool doublePulse)
    {
        // Ensure channels exist on O+
        NotificationHelper.EnsureChannels(ctx);

        var channelId = doublePulse ? ChannelEventsDouble : ChannelEventsShort;

        var builder = new NotificationCompat.Builder(ctx, channelId)
            .SetSmallIcon(AndroidRes.Mipmap.appicon)
            .SetContentTitle(title)
            .SetContentText(text)
            .SetPriority(NotificationCompat.PriorityHigh)
            .SetCategory(NotificationCompat.CategoryMessage)
            .SetAutoCancel(true)
            .SetOnlyAlertOnce(false);

        NotificationManagerCompat.From(ctx).Notify(Java.Util.UUID.RandomUUID().GetHashCode(), builder.Build());
    }
}
