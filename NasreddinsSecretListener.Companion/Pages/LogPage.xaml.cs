// Pages/LogPage.xaml.cs
using Microsoft.Maui.Controls;
using NasreddinsSecretListener.Companion.ViewModels;

namespace NasreddinsSecretListener.Companion.Pages;

public partial class LogPage : ContentPage
{
    public LogPage(LogViewModel vm)
    {
        InitializeComponent();
        BindingContext = vm;
    }
}