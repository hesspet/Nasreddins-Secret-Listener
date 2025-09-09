using Android.App;
using Android.Content;
using Android.Content.PM; // ForegroundService (TypeConnectedDevice)
using Android.OS;
using AndroidX.Core.App;
using Microsoft.Extensions.DependencyInjection;   // für GetService<T>()
using Microsoft.Maui.Storage;                    // Preferences
using NasreddinsSecretListener.Companion.Platforms.Android;
using NasreddinsSecretListener.Companion.Services;
using System;
using System.Runtime.Versioning;
using System.Threading;
using System.Threading.Tasks;
using AndroidRes = NasreddinsSecretListener.Companion.Resource;

namespace NasreddinsSecretListener.Companion;

[Service(
    Name = "de.hesspet.nsl.NslBleForegroundService",
    Exported = false
)]
[SupportedOSPlatform("android26.0")] // gesamte Klasse ist Android (ab 26)
public sealed class NslBleForegroundService : Service
{
    public static string ActionStart => $"{GetPackageName()}.action.START";

    // ---- Public API ---------------------------------------------------------
    public static string ActionStop => $"{GetPackageName()}.action.STOP";

    public static void Start(Context context)
    {
        if (context is null)
            throw new ArgumentNullException(nameof(context));

        var i = new Intent(context, typeof(NslBleForegroundService)).SetAction(ActionStart);

        // Analyzer-safe Guard (auch wenn minSdk=26 gesetzt ist)
        if (OperatingSystem.IsAndroidVersionAtLeast(26))
            context.StartForegroundService(i);
        else
            context.StartService(i);
    }

    public static void Stop(Context ctx)
    {
        // Variante 1: explizit StopService
        ctx.StopService(new Intent(ctx, typeof(NslBleForegroundService)));

        // Variante 2 (optional): über STOP-Action laufen lassen var stop = new Intent(ctx,
        // typeof(NslBleForegroundService)).SetAction(ACTION_STOP); if (Build.VERSION.SdkInt >=
        // BuildVersionCodes.O) ctx.StartForegroundService(stop); else ctx.StartService(stop);
    }

    public override IBinder? OnBind(Intent? intent) => null;

    // ---- Lifecycle ----------------------------------------------------------
    public override void OnCreate()
    {
        base.OnCreate();
        // Channel für Foreground-Service sicherstellen
        NotificationHelper.EnsureForegroundChannel(this);

        // PendingIntent zum Öffnen der App (optional, aber hübsch)
        var openIntent = new Intent(this, typeof(MainActivity))
            .AddFlags(ActivityFlags.SingleTop | ActivityFlags.ClearTop);
        var openPending = PendingIntent.GetActivity(
            this, 1000, openIntent, PendingIntentFlags.Immutable | PendingIntentFlags.UpdateCurrent);

        // SmallIcon muss gültig sein!
        var smallIcon = Resource.Mipmap.appicon;
        if (smallIcon == 0)
            smallIcon = global::Android.Resource.Drawable.StatNotifyMore; // Fallback

        var builder = new NotificationCompat.Builder(this, NotificationHelper.ChannelForeground)
            .SetSmallIcon(smallIcon)
            .SetContentTitle("NSL Companion läuft")
            .SetContentText("Lauscht auf Geräte…")
            .SetOngoing(true)
            .SetOnlyAlertOnce(true)
            .SetCategory(NotificationCompat.CategoryService)
            .SetForegroundServiceBehavior(NotificationCompat.ForegroundServiceImmediate)
            .SetContentIntent(openPending);

        var notification = builder.Build();

        // *** Muss sehr früh passieren ***
        StartForeground(1, notification);
    }

    public override void OnDestroy()
    {
        try
        {
            _cts?.Cancel();
        }
        catch { }
        _cts?.Dispose();
        _cts = null;
        base.OnDestroy();
    }

    public override StartCommandResult OnStartCommand(Intent? intent, StartCommandFlags flags, int startId)
    {
        if (intent?.Action == ACTION_STOP)
        {
            try
            {
                StopForeground(StopForegroundFlags.Remove);
            }
            catch { /* best effort */ }

            StopSelf();
            return StartCommandResult.NotSticky;
        }

        // bisherige Logik beibehalten:
        _cts ??= new CancellationTokenSource();
        _ = RunWorkerAsync(_cts.Token); // dein bestehender Worker
        return StartCommandResult.Sticky;
    }

    private const string ACTION_STOP = "de.hesspet.nsl.ACTION_STOP";

    private const string CHANNEL_ID = "nsl_ble_channel";
    private const int NOTIFICATION_ID = 1001;
    private CancellationTokenSource? _cts;
    // ---- Helpers ------------------------------------------------------------

    private static string GetPackageName()
        => Android.App.Application.Context?.PackageName ?? "de.hesspet.nsl";

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
            // Kurzer Delay: stellt sicher, dass MAUI-ServiceProvider initialisiert ist
            await Task.Delay(200, ct);

            var sp = MauiApplication.Current?.Services;
            var settings = sp?.GetService<ISettingsService>();
            var ble = sp?.GetService<IBleClient>();

            if (settings?.AutoConnectMyDevice == true && ble is not null)
            {
                // Dieselben Key wie in der VM verwenden
                const string MyDeviceKey = "nsl.myDeviceId";
                var myId = Preferences.Get(MyDeviceKey, string.Empty);

                // Non-blocking im Hintergrund verbinden; UI-Navigation übernimmt die VM bei "Verbunden..."
                _ = Task.Run(async () =>
                {
                    try
                    {
                        var ok = await ble.ConnectByIdOrScanAsync(myId, TimeSpan.FromSeconds(12));
                        Android.Util.Log.Info("NSL", $"AutoConnect (Service) Ergebnis: {ok}");
                    }
                    catch (Exception ex)
                    {
                        Android.Util.Log.Warn("NSL", $"AutoConnect (Service) Fehler: {ex}");
                    }
                }, ct);
            }

            // Hintergrundloop/Session starten (bestehend)
            await BleBackgroundSession.StartAsync(ct);
        }
        catch (System.OperationCanceledException)
        {
            // normal beim Stop
        }
        catch (Exception ex)
        {
            Android.Util.Log.Warn("NSL", $"Service-Worker Fehler: {ex}");
        }
    }
}
