namespace NasreddinsSecretListener.Companion.Models;

using CommunityToolkit.Mvvm.ComponentModel;

public partial class NslDevice : ObservableObject
{
    public NslDevice(string id, string name, int rssi,
                     bool isNsl = false, string? shortId = null,
                     string? mac = null, byte? status = null, bool isMine = false)
    {
        Id = id;
        Name = name;
        Rssi = rssi;
        _isNsl = isNsl;
        _shortId = shortId;
        _mac = mac;
        _status = status;
        IsMine = isMine;
    }

    // Feste Kennung (nicht änderbar)
    public string Id { get; }

    public string Name { get; }

    [ObservableProperty] private bool _isMine;

    [ObservableProperty] private bool _isNsl;

    [ObservableProperty] private string? _mac;

    // Änderbare Properties (lösen automatisch PropertyChanged aus)
    [ObservableProperty] private int _rssi;

    [ObservableProperty] private string? _shortId;
    [ObservableProperty] private byte? _status;
}