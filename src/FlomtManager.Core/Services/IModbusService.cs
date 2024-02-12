using FlomtManager.Core.Enums;
using FlomtManager.Core.Models;
using FlomtManager.Modbus;

namespace FlomtManager.Core.Services
{
    public interface IModbusService
    {
        Task<DeviceDefinition> ReadDeviceDefinition(IModbusProtocol modbusProtocol, byte slaveId, CancellationToken cancellationToken);
        Task<IEnumerable<Parameter>> ReadParameterDefinitions(IModbusProtocol modbusProtocol, byte slaveId, DeviceDefinition deviceDefinition, CancellationToken ct);
        Task<float[]?> ReadCurrentParameters(IModbusProtocol modbusProtocol, byte slaveId, DeviceDefinition deviceDefinition, CancellationToken ct);
        (ParameterType type, float comma) ParseParameterTypeByte(byte parameterTypeByte);
    }
}
