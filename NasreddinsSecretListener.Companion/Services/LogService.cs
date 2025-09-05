// Services/ILogService.cs

using Microsoft.Extensions.Logging;
using System.Collections.ObjectModel;
using System.Text;

namespace NasreddinsSecretListener.Companion.Services;

public interface ILogService
{
    // Read-only View für die UI
    ReadOnlyObservableCollection<LogEntry> Entries
    {
        get;
    }

    // Verwaltung
    void Clear();

    // Logging-API
    void Debug(string message, string? tag = null);

    void Error(string message, string? tag = null, System.Exception? ex = null);

    Task<string> ExportAsTextAsync();

    void Info(string message, string? tag = null);

    void Warn(string message, string? tag = null);

    // liefert den Text (für Share)
}

public sealed class LogEntry
{
    public string? Exception
    {
        get; init;
    }

    public LogLevel Level
    {
        get; init;
    }

    public string Message { get; init; } = "";

    public string? Tag
    {
        get; init;
    }

    public DateTime Timestamp { get; init; } = DateTime.UtcNow;
    // als Text (optional)
}

public sealed class LogService : ILogService
{
    public LogService(ILogger<LogService> logger)
    {
        _logger = logger;
        Entries = new ReadOnlyObservableCollection<LogEntry>(_entries);
    }

    public ReadOnlyObservableCollection<LogEntry> Entries
    {
        get;
    }

    public void Clear()
    {
        MainThread.BeginInvokeOnMainThread(() => _entries.Clear());
    }

    public void Debug(string message, string? tag = null) => Add(LogLevel.Debug, message, tag, null);

    public void Error(string message, string? tag = null, Exception? ex = null)
        => Add(LogLevel.Error, message, tag, ex);

    public async Task<string> ExportAsTextAsync()
    {
        // einfache Text-Repräsentation
        var sb = new StringBuilder();
        foreach (var e in Entries.ToList()) // Snapshot
        {
            var ts = e.Timestamp.ToLocalTime().ToString("yyyy-MM-dd HH:mm:ss.fff");
            var tag = string.IsNullOrWhiteSpace(e.Tag) ? "" : $" [{e.Tag}]";
            sb.AppendLine($"{ts} {e.Level,-5}{tag}  {e.Message}");
            if (!string.IsNullOrEmpty(e.Exception))
                sb.AppendLine($"    EX: {e.Exception}");
        }
        return await Task.FromResult(sb.ToString());
    }

    public void Info(string message, string? tag = null) => Add(LogLevel.Information, message, tag, null);

    public void Warn(string message, string? tag = null) => Add(LogLevel.Warning, message, tag, null);

    private const int MaxEntries = 200;

    // interne Collection + ReadOnly-Wrapper für Bindings
    private readonly ObservableCollection<LogEntry> _entries = new();

    private readonly ILogger<LogService> _logger;

    private void Add(LogLevel level, string message, string? tag, Exception? ex)
    {
        if (tag is not null)
        {
            _logger.Log(level, ex, "{Message} [{Tag}]", message, tag);
        }
        else
        {
            _logger.Log(level, ex, "{Message}", message);
        }

        var entry = new LogEntry
        {
            Level = level,
            Message = message,
            Tag = tag,
            Exception = ex?.ToString()
        };

        MainThread.BeginInvokeOnMainThread(() =>
        {
            _entries.Add(entry);

            // Ringpuffer begrenzen
            while (_entries.Count > MaxEntries)
                _entries.RemoveAt(0);
        });
    }
}
