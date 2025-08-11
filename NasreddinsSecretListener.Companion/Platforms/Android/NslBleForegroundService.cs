using Android.App;
using Android.Content;
using Android.OS;
using AndroidX.Core.App;
using System;
using System.Threading;
using System.Threading.Tasks;
using NasreddinsSecretListener.Companion.Services; // BleBackgroundSession
using AndroidRes = NasreddinsSecretListener.Companion.Resource;
using Android.Content.PM; // ForegroundService (TypeConnectedDevice etc.)

namespace NasreddinsSecretListener.Companion;

[Service(
    // <-- sprechender Android-/Java-Name, passt zum Manifest
    Name = "de.hesspet.nsl.NslBleForegroundService",
    Exported = false
)]
public class NslBleForegroundService : Service
{
    // Actions dynamisch aus dem echten Package bauen
    public static string ACTION_START => $"{PackageName}.action.START";

    public static string ACTION_STOP => $"{PackageName}.action.STOP";

    // Public helper zum Start/Stop aus App-Code
    public static void Start(Context context)
    {
        var i = new Intent(context, typeof(NslBleForegroundService));
        i.SetAction(ACTION_START);
        if (Build.VERSION.SdkInt >= BuildVersionCodes.O)
            context.StartForegroundService(i);
        else
            context.StartService(i);
    }

    public static void Stop(Context context)
    {
        var i = new Intent(context, typeof(NslBleForegroundService));
        i.SetAction(ACTION_STOP);
        context.StartService(i);
    }

    public override IBinder? OnBind(Intent? intent) => null;

    public override void OnCreate()
    {
        base.OnCreate();
        CreateNotificationChannel();
    }

    public override void OnDestroy()
    {
        _cts?.Cancel();
        _cts = null;
        base.OnDestroy();
    }

    public override StartCommandResult OnStartCommand(Intent? intent, StartCommandFlags flags, int startId)
    {
        var action = intent?.Action ?? ACTION_START;

        if (action == ACTION_STOP)
        {
            StopForeground(true);
            StopSelf();
            return StartCommandResult.NotSticky;
        }

        // Dauerhafte Notification für den Foreground Service
        var notification = new NotificationCompat.Builder(this, CHANNEL_ID)
            .SetContentTitle("NSL Companion läuft")
            .SetContentText("Bluetooth-Verbindung wird im Hintergrund gehalten.")
            .SetSmallIcon(AndroidRes.Drawable.ic_stat_nsl) // Status-Icon aus deinen Android-Ressourcen
            .SetOngoing(true)
            .SetOnlyAlertOnce(true)
            .Build();

        // StartForeground – ab API 34 Typ zwingend angeben (muss zum Manifest passen)
        if (Build.VERSION.SdkInt >= BuildVersionCodes.UpsideDownCake) // API 34+
        {
            StartForeground(
                NOTIFICATION_ID,
                notification,
                ForegroundService.TypeConnectedDevice
            );
        }
        else
        {
            StartForeground(NOTIFICATION_ID, notification);
        }

        // Worker starten (BLE-Loop)
        _cts ??= new CancellationTokenSource();
        _ = Task.Run(() => RunWorkerAsync(_cts.Token));

        return StartCommandResult.Sticky;
    }

    private const string CHANNEL_ID = "nsl_ble_channel";

    private const int NOTIFICATION_ID = 1001;

    private CancellationTokenSource? _cts;

    // PackageName zur Laufzeit aus dem Context (Fallback auf dein Package)
    private static string PackageName => Android.App.Application.Context?.PackageName ?? "de.hesspet.nsl";

    private void CreateNotificationChannel()
    {
        if (Build.VERSION.SdkInt >= BuildVersionCodes.O)
        {
            var channel = new NotificationChannel(CHANNEL_ID, "BLE-Hintergrunddienst", NotificationImportance.Low)
            {
                Description = "Hält die Bluetooth-Verbindung im Hintergrund."
            };
            var mgr = (NotificationManager?)GetSystemService(NotificationService);
            mgr?.CreateNotificationChannel(channel);
        }
    }

    private async Task RunWorkerAsync(CancellationToken ct)
    {
        try
        {
            // Starte/halte die BLE-Session im Hintergrund
            await BleBackgroundSession.StartAsync(ct);
        }
        catch (System.OperationCanceledException) { /* normal beim Stop */ }
        catch (Exception ex)
        {
            Android.Util.Log.Warn("NSL", $"Service-Worker Fehler: {ex}");
        }
    }
}