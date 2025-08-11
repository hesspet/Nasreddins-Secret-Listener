using Android;
using Android.App;
using Android.Content.PM;
using Android.OS;
using System.Collections.Generic;

namespace NasreddinsSecretListener.Companion.Platforms.Android;

public static class PermissionRequester
{
    public static void RequestAllIfNecessary(Activity activity)
    {
        if (Build.VERSION.SdkInt < BuildVersionCodes.M)
            return; // Runtime-Permissions erst ab API 23 nötig

        var required = GetRequiredPermissions();
        var optional = GetOptionalPermissions();

        var allPerms = new List<string>();
        allPerms.AddRange(required);
        allPerms.AddRange(optional);

        var missing = new List<string>();

        foreach (var p in allPerms)
        {
            if (activity.CheckSelfPermission(p) != Permission.Granted)
                missing.Add(p);
        }

        if (missing.Count > 0)
        {
            activity.RequestPermissions(missing.ToArray(), requestCode: 101);
        }
    }

    private static IEnumerable<string> GetOptionalPermissions()
    {
        var perms = new List<string>();

        // 33+ PostNotifications
        if (OperatingSystem.IsAndroidVersionAtLeast(33))
        {
            perms.Add(Manifest.Permission.PostNotifications);
        }

        return perms;
    }

    private static IEnumerable<string> GetRequiredPermissions()
    {
        var perms = new List<string>();

        // 31+ BLE Runtime-Permissions
        if (OperatingSystem.IsAndroidVersionAtLeast(31))
        {
            perms.Add(Manifest.Permission.BluetoothScan);
            perms.Add(Manifest.Permission.BluetoothConnect);
            perms.Add(Manifest.Permission.BluetoothAdvertise);
        }
        else
        {
            // Vor Android 12
            perms.Add(Manifest.Permission.Bluetooth);
            perms.Add(Manifest.Permission.BluetoothAdmin);
            perms.Add(Manifest.Permission.AccessFineLocation); // nötig fürs Scannen
        }

        return perms;
    }
}