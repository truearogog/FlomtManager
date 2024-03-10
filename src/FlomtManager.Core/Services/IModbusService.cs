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

        object ParseBytesToValue(ReadOnlySpan<byte> bytes, ParameterType type, byte comma);
        string StringParseBytesToValue(ReadOnlySpan<byte> bytes, ParameterType type, byte comma);

        string FloatToString(float value, byte comma);
        float ParseS16C(ReadOnlySpan<byte> bytes, byte comma);

        string StringParseS16C(ReadOnlySpan<byte> bytes, byte comma);
        float ParseU16C(ReadOnlySpan<byte> bytes, byte comma);

        string StringParseU16C(ReadOnlySpan<byte> bytes, byte comma);
        float ParseFS16C(ReadOnlySpan<byte> bytes, byte comma);

        string StringParseFS16C(ReadOnlySpan<byte> bytes, byte comma);
        float ParseFU16C(ReadOnlySpan<byte> bytes, byte comma);

        string StringParseFU16C(ReadOnlySpan<byte> bytes, byte comma);
        float ParseS32C(ReadOnlySpan<byte> bytes, byte comma, byte trim);

        string StringParseS32C(ReadOnlySpan<byte> bytes, byte comma, byte trim);
        string StringParseSeconds(ReadOnlySpan<byte> bytes);
        TimeSpan ParseSeconds(ReadOnlySpan<byte> bytes);
        DateTime ParseDateTime(ReadOnlySpan<byte> bytes);
        string StringParseDateTime(ReadOnlySpan<byte> bytes);
    }
}
