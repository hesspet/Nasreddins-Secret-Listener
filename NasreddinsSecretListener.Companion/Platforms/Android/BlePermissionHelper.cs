using System;
using Android; // für Manifest.Permission.*
using AndroidX.Core.Content; // ContextCompat

// WICHTIG: Alias auf den globalen Android-Namespace, um den Namespace-Konflikt zu vermeiden
using A = global::Android;

namespace NasreddinsSecretListener.Companion.Platforms.Android;

public static class BlePermissionHelper
{
    /// <summary>
    /// Liefert die für die aktuelle Android-Version notwendigen BLE-Berechtigungen.
    /// </summary>
    public static string[] GetRequiredPermissions()
    {
        if (OperatingSystem.IsAndroidVersionAtLeast(31))
        {
            // Android 12+ (API 31): neue feingranulare BLE-Permissions
            return new[]
            {
                Manifest.Permission.BluetoothScan,
                Manifest.Permission.BluetoothConnect,
                Manifest.Permission.BluetoothAdvertise
            };
        }
        else
        {
            // < Android 12: Location genügt für BLE-Scan
            return new[]
            {
                Manifest.Permission.AccessFineLocation
            };
        }
    }

    /// <summary>
    /// Prüft, ob alle benötigten Permissions erteilt sind.
    /// Nutzt Application.Context + ContextCompat → keine Activity nötig.
    /// </summary>
    public static bool HasAllPermissions()
    {
        var ctx = A.App.Application.Context;
        if (ctx is null) return false;

        var permissions = GetRequiredPermissions();
        foreach (var p in permissions)
        {
            if (ContextCompat.CheckSelfPermission(ctx, p) != A.Content.PM.Permission.Granted)
                return false;
        }
        return true;
    }
}