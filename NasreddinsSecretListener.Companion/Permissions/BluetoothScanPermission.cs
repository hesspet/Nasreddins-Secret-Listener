using Microsoft.Maui.ApplicationModel;

namespace NasreddinsSecretListener.Companion.PermissionsEx;

public class BluetoothScanPermission : Permissions.BasePlatformPermission
{
#if ANDROID

    public override (string androidPermission, bool isRuntime)[] RequiredPermissions => new[]
    {
        ("android.permission.BLUETOOTH_SCAN", true),
        ("android.permission.BLUETOOTH_CONNECT", true)
    };

#endif
}