using Android.App;
using Android.Content.PM;
using Android.OS;

namespace NasreddinsSecretListener.Companion;

[Activity(
    Theme = "@style/Maui.SplashTheme",
    MainLauncher = true,               // <-- Wichtig!
    ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation | ConfigChanges.UiMode |
                           ConfigChanges.ScreenLayout | ConfigChanges.SmallestScreenSize | ConfigChanges.Density)]
public class MainActivity : MauiAppCompatActivity
{
}