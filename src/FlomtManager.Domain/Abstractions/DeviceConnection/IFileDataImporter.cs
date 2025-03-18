using FlomtManager.Domain.Abstractions.DeviceConnection.Events;

namespace FlomtManager.Domain.Abstractions.DeviceConnection;

public interface IFileDataImporter
{
    event EventHandler<DeviceConnectionStateChangedArgs> OnConnectionStateChanged;
    event EventHandler<DeviceConnectionErrorArgs> OnConnectionError;
    event EventHandler<DeviceConnectionDataChangedArgs> OnConnectionDataChanged;
    event EventHandler<DeviceConnectionArchiveReadingStateChangedArgs> OnConnectionArchiveReadingStateChanged;
    event EventHandler<DeviceConnectionArchiveReadingProgressChangedArgs> OnConnectionArchiveReadingProgressChangedArgs;

    Task Import(Stream stream, CancellationToken cancellationToken = default);
}
