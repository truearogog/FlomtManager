namespace FlomtManager.Modbus
{
    public interface IModbusProtocol : IAsyncDisposable
    {
        bool IsOpen { get; }

        ValueTask OpenAsync(CancellationToken cancellationToken = default);
        ValueTask CloseAsync(CancellationToken cancellationToken = default);
        Task<ushort[]> ReadRegistersAsync(byte slaveId, ushort start, ushort count, Action<int, int> progressHandler = null, CancellationToken cancellationToken = default);
        Task<byte[]> ReadRegistersBytesAsync(byte slaveId, ushort start, ushort count, Action<int, int> progressHandler = null, CancellationToken cancellationToken = default);
    }
}
