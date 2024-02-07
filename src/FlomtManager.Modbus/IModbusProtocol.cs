namespace FlomtManager.Modbus
{
    public interface IModbusProtocol
    {
        bool IsOpen { get; }
        Task<bool> OpenAsync();
        void Close();
        ushort[] ReadRegisters(byte slaveId, ushort start, ushort count, CancellationToken ct);
        byte[] ReadRegistersBytes(byte slaveId, ushort start, ushort count, CancellationToken ct);
    }
}
