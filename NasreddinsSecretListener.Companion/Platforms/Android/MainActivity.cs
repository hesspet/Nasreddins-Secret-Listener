using Android.App;
using Android.Content.PM;
using Android.OS;
using NasreddinsSecretListener.Companion.Platforms.Android;

namespace NasreddinsSecretListener.Companion;

[Activity(
    Theme = "@style/Maui.SplashTheme",
    MainLauncher = true,
    ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation | ConfigChanges.UiMode |
                           ConfigChanges.ScreenLayout | ConfigChanges.SmallestScreenSize | ConfigChanges.Density)]
public class MainActivity : MauiAppCompatActivity
{
    protected override void OnCreate(Bundle? savedInstanceState)
    {
        base.OnCreate(savedInstanceState);

        // Runtime Permissions (Scan/Connect/Notifications etc.)
        PermissionRequester.RequestAllIfNecessary(this);

        // Foreground Service starten (nur einmal)
        NslBleForegroundService.Start(this);
    }
}