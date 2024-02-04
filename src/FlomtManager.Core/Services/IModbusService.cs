using FlomtManager.Core.Enums;
using FlomtManager.Core.Models;
using FlomtManager.Modbus;

namespace FlomtManager.Core.Services
{
    public interface IModbusService
    {
        DeviceDefinition ReadDeviceDefinitions(IModbusProtocol modbusProtocol, byte slaveId, CancellationToken ct);
        IEnumerable<Parameter> ReadParameterDefinitions(IModbusProtocol modbusProtocol, byte slaveId, DeviceDefinition deviceDefinition, CancellationToken ct);
        float[]? ReadCurrentParameters(IModbusProtocol modbusProtocol, byte slaveId, DeviceDefinition deviceDefinition, CancellationToken ct);
        (ParameterType type, float comma) ParseParameterTypeByte(byte parameterTypeByte);
    }
}
