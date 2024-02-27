using FlomtManager.Core.Enums;
using FlomtManager.Core.Models;
using FlomtManager.Modbus;

namespace FlomtManager.Core.Services
{
    public interface IModbusService
    {
        DeviceDefinition ParseDeviceDefinition(ReadOnlySpan<ushort> registers);
        Task<DeviceDefinition> ReadDeviceDefinition(
            IModbusProtocol modbusProtocol, byte slaveId, CancellationToken cancellationToken = default);

        IEnumerable<Parameter> ParseParameterDefinitions(ReadOnlySpan<byte> bytes);
        Task<IEnumerable<Parameter>> ReadParameterDefinitions(
            IModbusProtocol modbusProtocol, byte slaveId, DeviceDefinition deviceDefinition, CancellationToken cancellationToken = default);

        (ParameterType Type, byte Comma) ParseParameterTypeByte(byte parameterTypeByte);
        float GetComma(byte commaByte);
    }
}
