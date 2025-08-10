using Microsoft.Maui.Controls;

namespace NasreddinsSecretListener.Companion;

public partial class App : Application
{
    public App()
    {
        InitializeComponent();
        MainPage = new AppShell();
    }
}
