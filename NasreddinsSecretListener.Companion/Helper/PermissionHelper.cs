using NasreddinsSecretListener.Companion.PermissionsEx;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.NetworkInformation;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Maui.ApplicationModel;

namespace NasreddinsSecretListener.Companion.Helper;

internal static class PermissionHelper
{
    public static async Task EnsureBleScanAsync()
    {
#if ANDROID
        if (OperatingSystem.IsAndroidVersionAtLeast(31))
        {
            var status = await Permissions.CheckStatusAsync<BluetoothScanPermission>();
            if (status != PermissionStatus.Granted)
                status = await Permissions.RequestAsync<BluetoothScanPermission>();

            if (status != PermissionStatus.Granted)
                throw new PermissionException("BLUETOOTH_SCAN/CONNECT nicht gewährt.");
        }
        else
        {
            // Vor Android 12 braucht man Location (für BLE-Scan).
            var loc = await Permissions.CheckStatusAsync<Permissions.LocationWhenInUse>();
            if (loc != PermissionStatus.Granted)
                loc = await Permissions.RequestAsync<Permissions.LocationWhenInUse>();

            if (loc != PermissionStatus.Granted)
                throw new PermissionException("Standortberechtigung nicht gewährt (für BLE-Scan erforderlich).");
        }
#endif
    }
}