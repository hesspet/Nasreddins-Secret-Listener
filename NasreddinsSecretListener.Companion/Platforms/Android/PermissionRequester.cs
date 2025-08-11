// Auto-added PermissionRequester for runtime permissions
using Android;
using Android.App;
using Android.OS;
using AndroidX.Core.App;
using AndroidX.Core.Content;
using System.Collections.Generic;
using System.Linq;

namespace NasreddinsSecretListener.Companion;

public static class PermissionRequester
{
    static readonly string[] PreS_Required = new[] {
        Manifest.Permission.AccessFineLocation, // für BLE <= Android 11
    };

    static readonly string[] S_Required = new[] {
        Manifest.Permission.BluetoothScan,
        Manifest.Permission.BluetoothConnect,
        Manifest.Permission.BluetoothAdvertise,
    };

    static readonly string[] Tiramisu_Optional = new[] {
        Manifest.Permission.PostNotifications,
    };

    public static void RequestAllIfNecessary(Activity activity)
    {
        var toRequest = new List<string>();

        if (Build.VERSION.SdkInt >= BuildVersionCodes.S) // 31+
        {
            foreach (var p in S_Required)
                if (ContextCompat.CheckSelfPermission(activity, p) != Android.Content.PM.Permission.Granted)
                    toRequest.Add(p);
        }
        else
        {
            foreach (var p in PreS_Required)
                if (ContextCompat.CheckSelfPermission(activity, p) != Android.Content.PM.Permission.Granted)
                    toRequest.Add(p);
        }

        if (Build.VERSION.SdkInt >= BuildVersionCodes.Tiramisu) // 33+
        {
            foreach (var p in Tiramisu_Optional)
                if (ContextCompat.CheckSelfPermission(activity, p) != Android.Content.PM.Permission.Granted)
                    toRequest.Add(p);
        }

        if (toRequest.Count > 0)
        {
            ActivityCompat.RequestPermissions(activity, toRequest.Distinct().ToArray(), 1001);
        }
    }
}