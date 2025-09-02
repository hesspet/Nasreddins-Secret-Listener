using NasreddinsSecretListener.Companion.ViewModels;

namespace NasreddinsSecretListener.Companion.Pages;

public partial class SettingsPage : ContentPage
{
    public SettingsPage(SettingsViewModel vm)
    {
        InitializeComponent();
        BindingContext = vm;
    }
}