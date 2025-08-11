using CommunityToolkit.Maui;
using NasreddinsSecretListener.Companion.Pages;
using NasreddinsSecretListener.Companion.Services;
using NasreddinsSecretListener.Companion.ViewModels;

namespace NasreddinsSecretListener.Companion;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();

        builder
            .UseMauiApp<App>()
            .UseMauiCommunityToolkit()
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
            });

        // ViewModels
        builder.Services.AddSingleton<ScanViewModel>();
        builder.Services.AddSingleton<SettingsViewModel>();

        // clients
        builder.Services.AddSingleton<IBleClient, BleClientService>();

        // Pages
        builder.Services.AddTransient<ScanPage>();
        builder.Services.AddTransient<StatusPage>();
        builder.Services.AddTransient<SettingsPage>();

        return builder.Build();
    }
}