using NasreddinsSecretListener.Companion.Models;

namespace NasreddinsSecretListener.Companion.Services;

public interface IBleClient
{
    event Action<NslDevice> DeviceDiscovered;

    event Action<string> StateChanged;

    Task<bool> ConnectAndSubscribeAsync(string deviceId);

    // ▼ NEU:
    Task<bool> ConnectByIdOrScanAsync(string deviceId, TimeSpan timeout);

    Task DisconnectAsync();

    Task StartScanAsync();

    Task StopScanAsync();
}