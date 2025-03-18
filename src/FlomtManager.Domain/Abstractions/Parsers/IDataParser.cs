using System.Collections.ObjectModel;
using FlomtManager.Domain.Enums;
using FlomtManager.Domain.Models;

namespace FlomtManager.Domain.Abstractions.Parsers;

public interface IDataParser
{
    ReadOnlyCollection<Parameter> ParseParameterDefinition(ReadOnlySpan<byte> bytes, int deviceId);
    (ParameterType Type, byte Comma) ParseParameterTypeByte(byte parameterTypeByte);
    string ParseBytesToString(ReadOnlySpan<byte> bytes, Parameter parameter);
    float ParseBytesToFloat(ReadOnlySpan<byte> bytes, Parameter parameter);
    uint ParseBytesToUInt32(ReadOnlySpan<byte> bytes, Parameter parameter);
    ushort ParseBytesToUInt16(ReadOnlySpan<byte> bytes, Parameter parameter);
    TimeSpan ParseBytesToTimeSpan(ReadOnlySpan<byte> bytes, Parameter parameter);
    DateTime ParseBytesToDateTime(ReadOnlySpan<byte> bytes, Parameter parameter);

    ushort ParseUInt16(ReadOnlySpan<byte> bytes);
    DateTime ParseDateTime(ReadOnlySpan<byte> bytes);
}
