using Microsoft.Maui.Controls;
using NasreddinsSecretListener.Companion.ViewModels;

namespace NasreddinsSecretListener.Companion.Pages;

public partial class ScanPage : ContentPage
{
    public ScanPage(ScanViewModel vm)
    {
        InitializeComponent();
        BindingContext = vm;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        // Auto-Connect per Command (statt alter Methode)
        var vm = (ScanViewModel)BindingContext;
        await vm.TryAutoConnectCommand.ExecuteAsync(null);
    }
}