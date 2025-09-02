// ViewModels/LogViewModel.cs
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Maui.ApplicationModel.DataTransfer;
using NasreddinsSecretListener.Companion.Services;

namespace NasreddinsSecretListener.Companion.ViewModels;

public partial class LogViewModel : ObservableObject
{
    private readonly ILogService _log;

    public LogViewModel(ILogService log)
    {
        _log = log;
        Entries = _log.Entries;
    }

    public ReadOnlyObservableCollection<LogEntry> Entries { get; }

    [RelayCommand]
    private void Clear() => _log.Clear();

    [RelayCommand]
    private async Task ShareAsync()
    {
        var text = await _log.ExportAsTextAsync();
        await Share.Default.RequestAsync(new ShareTextRequest
        {
            Title = "NSL Log",
            Text = text
        });
    }
}
