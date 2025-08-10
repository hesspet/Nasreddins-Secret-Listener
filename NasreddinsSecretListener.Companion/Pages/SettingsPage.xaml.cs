using NasreddinsSecretListener.Companion.ViewModels;

namespace NasreddinsSecretListener.Companion.Pages;

public partial class SettingsPage : ContentPage
{
    public SettingsPage() // <- parameterlos!
    {
        InitializeComponent();
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        if (BindingContext is SettingsViewModel vm) vm.Load();
    }
}