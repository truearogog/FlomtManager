using FlomtManager.Domain.Abstractions.DeviceConnection.Events;

namespace FlomtManager.Domain.Abstractions.DeviceConnection;

public interface IDeviceConnection : IAsyncDisposable
{
    event EventHandler<DeviceConnectionStateChangedArgs> OnConnectionStateChanged;
    event EventHandler<DeviceConnectionDataChangedArgs> OnConnectionDataChanged;
    event EventHandler<DeviceConnectionErrorArgs> OnConnectionError;
    event EventHandler<DeviceConnectionArchiveReadingStateChangedArgs> OnConnectionArchiveReadingStateChanged;
    event EventHandler<DeviceConnectionArchiveReadingProgressChangedArgs> OnConnectionArchiveReadingProgressChangedArgs;

    Task Connect(CancellationToken cancellationToken = default);

    Task Disconnect(CancellationToken cancellationToken = default);
}
