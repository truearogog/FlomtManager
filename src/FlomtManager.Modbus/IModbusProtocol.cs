namespace FlomtManager.Modbus
{
    public interface IModbusProtocol
    {
        bool IsOpen { get; }

        ValueTask OpenAsync(CancellationToken cancellationToken);
        ValueTask CloseAsync(CancellationToken cancellationToken);
        Task<ushort[]> ReadRegistersAsync(byte slaveId, ushort start, ushort count, CancellationToken cancellationToken);
        Task<byte[]> ReadRegistersBytesAsync(byte slaveId, ushort start, ushort count, CancellationToken cancellationToken);
    }
}
