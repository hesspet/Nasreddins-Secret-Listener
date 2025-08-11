using Microsoft.Maui;
using Microsoft.Maui.Controls;

namespace NasreddinsSecretListener.Companion;

public partial class App : Application
{
    public App()
    {
        InitializeComponent();
    }

    // .NET 9: Root-Seite hier setzen statt MainPage
    protected override Window CreateWindow(IActivationState? activationState)
    {
        var window = new Window(new AppShell());
        // optional: window.Title = "NSL Companion";
        return window;
    }
}