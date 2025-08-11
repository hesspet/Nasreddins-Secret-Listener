using Microsoft.Maui.Controls;
using NasreddinsSecretListener.Companion.ViewModels;

namespace NasreddinsSecretListener.Companion.Pages;

public partial class StatusPage : ContentPage
{
    public StatusPage(ScanViewModel vm)
    {
        InitializeComponent();
        BindingContext = vm;
    }
}