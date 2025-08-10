using Microsoft.Maui.Controls;
using NasreddinsSecretListener.Companion.ViewModels;

namespace NasreddinsSecretListener.Companion.Pages;

public partial class ScanPage : ContentPage
{
    // MAUI DI injiziert ScanViewModel, weil es in MauiProgram.cs registriert ist
    public ScanPage(ScanViewModel vm)
    {
        InitializeComponent();
        _vm = vm;
        BindingContext = _vm;
    }

    // optional: Auto-Connect beim Anzeigen
    protected override async void OnAppearing()
    {
        base.OnAppearing();
        if (BindingContext is ScanViewModel vm)
        {
            await vm.LoadMyDeviceAsync();
            // Optional: gleich versuchen zu verbinden
            // if (vm.HasMyDevice) await vm.ConnectMyAsync();
        }
    }

    private readonly ScanViewModel _vm;

    private async void OnClaimClicked(object sender, EventArgs e)
    {
        if (BindingContext is ScanViewModel vm)
            await vm.ClaimSelectedAsync();
    }

    private async void OnConnectClicked(object sender, EventArgs e)
    {
        await _vm.ConnectAndListenAsync();
    }
}