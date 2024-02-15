namespace FlomtManager.Modbus
{
    public interface IModbusProtocol
    {
        bool IsOpen { get; }

        ValueTask OpenAsync(CancellationToken cancellationToken);
        ValueTask CloseAsync(CancellationToken cancellationToken);
        Task<ushort[]> ReadRegistersAsync(byte slaveId, ushort start, ushort count, Action<int, int>? progressHandler = null, CancellationToken cancellationToken = default);
        Task<byte[]> ReadRegistersBytesAsync(byte slaveId, ushort start, ushort count, Action<int, int>? progressHandler = null, CancellationToken cancellationToken = default);
    }
}
