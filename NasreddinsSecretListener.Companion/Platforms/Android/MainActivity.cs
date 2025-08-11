using Android.App;
using Android.Content.PM;

namespace NasreddinsSecretListener.Companion;

[Activity(
    Theme = "@style/Maui.SplashTheme",
    MainLauncher = true,               // <-- Wichtig!
    ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation | ConfigChanges.UiMode |
                           ConfigChanges.ScreenLayout | ConfigChanges.SmallestScreenSize | ConfigChanges.Density)]
public class MainActivity : MauiAppCompatActivity
{
    protected override void OnCreate(Android.OS.Bundle? savedInstanceState)
    {
        base.OnCreate(savedInstanceState);

        NasreddinsSecretListener.Companion.PermissionRequester.RequestAllIfNecessary(this);
        NasreddinsSecretListener.Companion.NslBleForegroundService.Start(this);
        // Runtime Permissions (Scan/Connect/Notifications etc.)
        PermissionRequester.RequestAllIfNecessary(this);

        NslBleForegroundService.Start(this);
        // Foreground Service starten
        NslBleForegroundService.Start(this);
    }
}