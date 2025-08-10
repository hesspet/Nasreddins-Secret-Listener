using CommunityToolkit.Maui;
using Microsoft.Maui;
using Microsoft.Maui.Controls.Hosting;
using Microsoft.Maui.Hosting;
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

        builder.Services.AddSingleton<IBleClient, BleClientService>();
        builder.Services.AddSingleton<ScanViewModel>();
        builder.Services.AddSingleton<ScanPage>();
        builder.Services.AddSingleton<StatusPage>();

        return builder.Build();
    }
}
