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

        builder.Services.AddSingleton<ISettingsService, SettingsService>();
        builder.Services.AddSingleton<IFeedbackService, FeedbackService>();
        builder.Services.AddSingleton<IBleClient, BleClientService>();

        builder.Services.AddSingleton<ScanViewModel>();
        builder.Services.AddTransient<ScanPage>();
        builder.Services.AddTransient<StatusPage>();
        builder.Services.AddTransient<SettingsViewModel>();
        builder.Services.AddTransient<SettingsPage>();
        return builder.Build();
    }
}