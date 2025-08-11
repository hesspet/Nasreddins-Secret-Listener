using System;
using System.Runtime.Versioning;
using System.Threading;
using System.Threading.Tasks;
using Android.App;
using Android.Content;
using Android.OS;
using AndroidX.Core.App;
using Android.Content.PM; // ForegroundService (TypeConnectedDevice)
using NasreddinsSecretListener.Companion.Services;
using AndroidRes = NasreddinsSecretListener.Companion.Resource;

namespace NasreddinsSecretListener.Companion;

[Service(
    Name = "de.hesspet.nsl.NslBleForegroundService",
    Exported = false
)]
[SupportedOSPlatform("android26.0")] // gesamte Klasse ist Android (ab 26)
public sealed class NslBleForegroundService : Service
{
    private const string CHANNEL_ID = "nsl_ble_channel";
    private const int NOTIFICATION_ID = 1001;
    private CancellationTokenSource? _cts;

    // ---- Public API ---------------------------------------------------------

    public static string ActionStart => $"{GetPackageName()}.action.START";
    public static string ActionStop => $"{GetPackageName()}.action.STOP";

    public static void Start(Context context)
    {
        if (context is null) throw new ArgumentNullException(nameof(context));

        var i = new Intent(context, typeof(NslBleForegroundService)).SetAction(ActionStart);

        // Analyzer-safe Guard (auch wenn minSdk=26 gesetzt ist)
        if (OperatingSystem.IsAndroidVersionAtLeast(26))
            context.StartForegroundService(i);
        else
            context.StartService(i);
    }

    public static void Stop(Context context)
    {
        if (context is null) throw new ArgumentNullException(nameof(context));
        var i = new Intent(context, typeof(NslBleForegroundService)).SetAction(ActionStop);
        context.StartService(i);
    }

    // ---- Lifecycle ----------------------------------------------------------

    public override IBinder? OnBind(Intent? intent) => null;

    public override void OnCreate()
    {
        base.OnCreate();
        CreateNotificationChannel(); // minSdk >= 26 garantiert verfügbar
    }

    public override void OnDestroy()
    {
        _cts?.Cancel();
        _cts = null;
        base.OnDestroy();
    }

    public override StartCommandResult OnStartCommand(Intent? intent, StartCommandFlags flags, int startId)
    {
        var action = intent?.Action ?? ActionStart;

        if (action == ActionStop)
        {
            // Ab 33 neuer Overload; darunter alter (nicht veralteter) bool-Overload
            if (OperatingSystem.IsAndroidVersionAtLeast(33))
                StopForeground(StopForegroundFlags.Remove);
            else
#pragma warning disable CA1422 // an dieser Stelle bewusst: nur <33
                StopForeground(true);
#pragma warning restore CA1422

            StopSelf();
            return StartCommandResult.NotSticky;
        }

        var notification = BuildNotification()!; // nie null

        // Ab 34 Typ mitgeben; darunter 2-Param-Overload
        if (OperatingSystem.IsAndroidVersionAtLeast(34))
        {
            StartForeground(NOTIFICATION_ID, notification, ForegroundService.TypeConnectedDevice);
        }
        else
        {
            StartForeground(NOTIFICATION_ID, notification);
        }

        _cts ??= new CancellationTokenSource();
        _ = Task.Run(() => RunWorkerAsync(_cts.Token));

        return StartCommandResult.Sticky;
    }

    // ---- Helpers ------------------------------------------------------------

    private static string GetPackageName()
        => Android.App.Application.Context?.PackageName ?? "de.hesspet.nsl";

    private Notification? BuildNotification()
    {
        // NotificationCompat.Builder(...).Build() liefert eine valide Notification
        // 'this' darf nicht null sein, aber der Compiler kann das nicht garantieren.
        // Daher mit Null-Prüfung absichern:
        if (this == null)
            throw new InvalidOperationException("Service-Kontext ist null.");

        // Zusätzliche Absicherung: Context explizit als non-null deklarieren
        var context = this;
        if (context is null)
            throw new InvalidOperationException("Context ist null.");

        // Explizit als non-null casten, um den Nullverweis-Fehler zu vermeiden
        return new NotificationCompat.Builder(context!, CHANNEL_ID)!
            .SetContentTitle("NSL Companion läuft")!
            .SetContentText("Bluetooth-Verbindung wird im Hintergrund gehalten.")!
            .SetSmallIcon(AndroidRes.Drawable.ic_stat_nsl)!
            .SetOngoing(true)!
            .SetOnlyAlertOnce(true)!
            .Build()!;
    }

    private void CreateNotificationChannel()
    {
        // minSdk=26 → NotificationChannel existiert sicher (keine Guards nötig)
        var channel = new NotificationChannel(
            CHANNEL_ID,
            "BLE-Hintergrunddienst",
            NotificationImportance.Low)
        {
            Description = "Hält die Bluetooth-Verbindung im Hintergrund."
        };

        var mgr = (NotificationManager?)GetSystemService(NotificationService);
        // mgr kann theoretisch null sein – daher Null-conditional:
        mgr?.CreateNotificationChannel(channel);
    }

    private async Task RunWorkerAsync(CancellationToken ct)
    {
        try
        {
            await BleBackgroundSession.StartAsync(ct);
        }
        catch (System.OperationCanceledException) { /* normal beim Stop */ }
        catch (Exception ex)
        {
            Android.Util.Log.Warn("NSL", $"Service-Worker Fehler: {ex}");
        }
    }
}