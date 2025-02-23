using System.Buffers.Binary;
using System.Text;
using FlomtManager.Core.Constants;
using FlomtManager.Core.Entities;
using FlomtManager.Core.Enums;
using FlomtManager.Core.Services;
using FlomtManager.Framework.Extensions;
using FlomtManager.Framework.Skia;
using FlomtManager.Modbus;

namespace FlomtManager.Infrastructure.Services;

internal sealed class ModbusService : IModbusService
{
    public DeviceDefinition ParseDeviceDefinition(ReadOnlySpan<ushort> registers)
    {
        var deviceDefinition = new DeviceDefinition
        {
            ParameterDefinitionStart = registers[0],
            ParameterDefinitionNumber = registers[1],
            DescriptionStart = registers[2],
            ProgramVersionStart = registers[3],

            CurrentParameterLineDefinitionStart = registers[4],
            CurrentParameterLineNumber = (byte)registers[5],
            CurrentParameterLineLength = (byte)(registers[5] >> 8),
            CurrentParameterLineStart = registers[6],

            IntegralParameterLineDefinitionStart = registers[7],
            IntegralParameterLineNumber = (byte)registers[8],
            IntegralParameterLineLength = (byte)(registers[8] >> 8),
            IntegralParameterLineStart = registers[9],

            AverageParameterArchiveLineDefinitionStart = registers[10],
            AverageParameterArchiveLineNumber = (byte)registers[11],
            AverageParameterArchiveLineLength = (byte)(registers[11] >> 8),

            AveragePerHourBlockStart = registers[14],
            AveragePerHourBlockLineCount = registers[15],
        };

        var bytes = deviceDefinition.SerializeBytes();
        deviceDefinition.CRC = ModbusHelper.GetCRC(bytes);

        return deviceDefinition;
    }

    public async Task<DeviceDefinition> ReadDeviceDefinition(IModbusProtocol modbusProtocol, byte slaveId, CancellationToken cancellationToken)
    {
        var registers = await modbusProtocol.ReadRegistersAsync(slaveId, 116, 42, cancellationToken: cancellationToken);
        return ParseDeviceDefinition(registers);
    }

    /*
    200.352 [C] "All parameters definition" 22 parameters * 16 one block length)
        X... Description of 1 parameter Bytes: (16 bytes per parameter)
        0 - Assigning a number to the parameter 0 - disabled
                    * this number will be used when describing parameter blocks
        1 - Assign parameter number after integration (0 - ordinary parameter) 
                    * for example: Q(m3/h)(INT16) after integration converted to V(m3)(INT32)
        2,3 - UINT16 error mask
        4,5 - reserved 
        6...9 -'Q',0,0,0 - symbolic name of the parameter - 4 characters max or up to 0
        10...15 - 'm3/h',0,0 - parameter units - 6 characters max or up to 0
    */
    public IReadOnlyList<Parameter> ParseParameterDefinitions(ReadOnlySpan<byte> bytes)
    {
        var hues = new List<float>();

        var parameters = new List<Parameter>();
        for (var i = 0; i < DeviceConstants.MAX_PARAMETER_COUNT; ++i)
        {
            var parameterBytes = bytes.Slice(i * 16, 16).ToArray();
            if (parameterBytes[0] != 0)
            {
                var (type, comma) = ParseParameterTypeByte(parameterBytes[1]);

                var color = RandomColor.Next(hues);
                hues.Add(color.Hue);
                var parameter = new Parameter
                {
                    Number = parameterBytes[0],
                    ParameterType = type,
                    Comma = comma,
                    ErrorMask = (ushort)(parameterBytes[2] + parameterBytes[3] << 8),
                    IntegrationNumber = parameterBytes[4],
                    Name = Encoding.ASCII.GetString(parameterBytes.Skip(6).TakeWhile(x => x != '\0').ToArray()),
                    Unit = Encoding.ASCII.GetString(parameterBytes.Skip(10).TakeWhile(x => x != '\0').ToArray()),
                    Color = color.ToString(),
                    ChartYScalingType = ChartScalingType.Auto,
                    ChartYZoom = 1,
                };

                parameters.Add(parameter);
            }
        }
        return parameters.AsReadOnly();
    }

    public async Task<IEnumerable<Parameter>> ReadParameterDefinitions(
        IModbusProtocol modbusProtocol, byte slaveId, DeviceDefinition deviceDefinition, CancellationToken cancellationToken = default)
    {
        var bytes = await modbusProtocol.ReadRegistersBytesAsync(slaveId, deviceDefinition.ParameterDefinitionStart,
            DeviceConstants.MAX_PARAMETER_COUNT * 16 / 2, cancellationToken: cancellationToken);
        return ParseParameterDefinitions(bytes);
    }

    /*
    b 0...2 comma position 0=0, 1=0.0, 2=0.00, 3=0.000, 4=0.0000, 7=*10
    b 3...6 0000.XXX(00-07) - S16C0...S16C7 - fixed comma signed,
            0001.XXX(08-15) - U16C0...U16C7 - fixed comma unsigned,
            0010.XXX(16-23) - FS16?0...FS16?7 - float 16 signed,
            0011.XXX(24-31) - FU16?0...FU16?7 - float 16 unsigned,
            0100.XXX(32-39) - S32C0...S32C7 - 32 bit value fixed comma
            0101.XXX(40-47) - S32C0D1...S32C7D1 - 32 bit value ignore 1 last digits
            0110.XXX(48-55) - S32C0D2...S32C7D2 - 32 bit value ignore 2 last digits
            0111.XXX(56-63) - S32C0D3...S32C7D3 - 32 bit value ignore 3 last digits
            01000.XXX(64-71) - S48C0...S48C7     - 64(48) bit value fixed coma(used 48 bits)
            01001.XXX(72-79) - S48C0D1...S48C7D1 - 64(48) bit value ignore 1 last digit
            01010.XXX(80-87) - S48C0D2...S48C7D2 - 64(48) bit value ignore 2 last digit
            01011.XXX(88-95) - S48C0D3...S48C7D3 - 64(48) bit value ignore 3 last digit
            96 -  16 bit unsigned Errors
            97 -  32 bit T working time in seconds (HHHH:MM:SS)
            98 -  16 bit unsigned working time in seconds in archive interval
            99 -  16 bit unsigned Working minutes in the archive interval
            100 - 16 bit unsigned Working hours in the archived interval
            101 - 8 bit * 6 SS:MM:HH DD.MM.YY
            102 - U32 seconds sine 2000 year
            103 - 127 - reserved
    */
    public (ParameterType Type, byte Comma) ParseParameterTypeByte(byte parameterTypeByte)
    {
        return parameterTypeByte switch
        {
            <= 7 => (ParameterType.S16C, GetCommaFromByte(parameterTypeByte)),
            >= 8 and <= 15 => (ParameterType.U16C, GetCommaFromByte(parameterTypeByte)),
            >= 16 and <= 23 => (ParameterType.FS16C, GetCommaFromByte(parameterTypeByte)),
            >= 24 and <= 31 => (ParameterType.FU16C, GetCommaFromByte(parameterTypeByte)),
            >= 32 and <= 39 => (ParameterType.S32C, GetCommaFromByte(parameterTypeByte)),
            >= 40 and <= 47 => (ParameterType.S32CD1, GetCommaFromByte(parameterTypeByte)),
            >= 48 and <= 55 => (ParameterType.S32CD2, GetCommaFromByte(parameterTypeByte)),
            >= 56 and <= 63 => (ParameterType.S32CD3, GetCommaFromByte(parameterTypeByte)),
            >= 64 and <= 71 => (ParameterType.S48C, GetCommaFromByte(parameterTypeByte)),
            >= 72 and <= 79 => (ParameterType.S48CD1, GetCommaFromByte(parameterTypeByte)),
            >= 80 and <= 87 => (ParameterType.S48CD2, GetCommaFromByte(parameterTypeByte)),
            >= 88 and <= 95 => (ParameterType.S48CD3, GetCommaFromByte(parameterTypeByte)),
            96 => (ParameterType.Error, 1),
            97 => (ParameterType.WorkingTimeInSeconds, 1),
            98 => (ParameterType.WorkingTimeInSecondsInArchiveInterval, 1),
            99 => (ParameterType.WorkingTimeInMinutesInArchiveInterval, 1),
            100 => (ParameterType.WorkingTimeInHoursInArchiveInterval, 1),
            101 => (ParameterType.Time, 1),
            102 => (ParameterType.SecondsSince2000, 1),
            _ => throw new NotSupportedException(),
        };
    }

    private static byte GetCommaFromByte(byte commaByte) => commaByte &= 0x7;

    public float GetComma(byte commaByte)
    {
        commaByte &= 0x7;
        return commaByte switch
        {
            0 => 1,
            >= 1 and <= 4 => MathF.Pow(10, -commaByte),
            7 => 10,
            _ => throw new NotSupportedException()
        };
    }

    public object ParseBytesToValue(ReadOnlySpan<byte> bytes, ParameterType type, byte comma)
    {
        return type switch
        {
            ParameterType.S16C => ParseS16C(bytes, comma),
            ParameterType.U16C => ParseU16C(bytes, comma),
            ParameterType.FS16C => ParseFS16C(bytes, comma),
            ParameterType.FU16C => ParseFU16C(bytes, comma),
            ParameterType.S32C => ParseS32C(bytes, comma, 0),
            ParameterType.S32CD1 => ParseS32C(bytes, comma, 1),
            ParameterType.S32CD2 => ParseS32C(bytes, comma, 2),
            ParameterType.S32CD3 => ParseS32C(bytes, comma, 3),
            ParameterType.S48C => ParseS48C(bytes, comma, 0), 
            ParameterType.S48CD1 => ParseS48C(bytes, comma, 1), 
            ParameterType.S48CD2 => ParseS48C(bytes, comma, 2), 
            ParameterType.S48CD3 => ParseS48C(bytes, comma, 3), 
            ParameterType.Error => BitConverter.ToUInt16(bytes),
            ParameterType.WorkingTimeInSeconds => BinaryPrimitives.ReadUInt32LittleEndian(bytes),
            ParameterType.WorkingTimeInSecondsInArchiveInterval => BitConverter.ToUInt16(bytes),
            ParameterType.WorkingTimeInMinutesInArchiveInterval => BitConverter.ToUInt16(bytes),
            ParameterType.WorkingTimeInHoursInArchiveInterval => BitConverter.ToUInt16(bytes),
            ParameterType.Time => ParseDateTime(bytes),
            ParameterType.SecondsSince2000 => BitConverter.ToUInt32(bytes),
            _ => string.Empty
        };
    }

    public string StringParseBytesToValue(ReadOnlySpan<byte> bytes, ParameterType type, byte comma)
    {
        return type switch
        {
            ParameterType.S16C => StringParseS16C(bytes, comma),
            ParameterType.U16C => StringParseU16C(bytes, comma),
            ParameterType.FS16C => StringParseFS16C(bytes, comma),
            ParameterType.FU16C => StringParseFU16C(bytes, comma),
            ParameterType.S32C => StringParseS32C(bytes, comma, 0),
            ParameterType.S32CD1 => StringParseS32C(bytes, comma, 1),
            ParameterType.S32CD2 => StringParseS32C(bytes, comma, 2),
            ParameterType.S32CD3 => StringParseS32C(bytes, comma, 3),
            ParameterType.S48C => StringParseS48C(bytes, comma, 0),
            ParameterType.S48CD1 => StringParseS48C(bytes, comma, 1),
            ParameterType.S48CD2 => StringParseS48C(bytes, comma, 2),
            ParameterType.S48CD3 => StringParseS48C(bytes, comma, 3),
            ParameterType.Error => BitConverter.ToUInt16(bytes).ToString(),
            ParameterType.WorkingTimeInSeconds => StringParseSeconds(bytes),
            ParameterType.WorkingTimeInSecondsInArchiveInterval => BitConverter.ToUInt16(bytes).ToString(),
            ParameterType.WorkingTimeInMinutesInArchiveInterval => BitConverter.ToUInt16(bytes).ToString(),
            ParameterType.WorkingTimeInHoursInArchiveInterval => BitConverter.ToUInt16(bytes).ToString(),
            ParameterType.Time => StringParseDateTime(bytes),
            ParameterType.SecondsSince2000 => BitConverter.ToUInt32(bytes).ToString(),
            _ => string.Empty
        };
    }

    public string FloatToString(float value, byte comma)
    {
        return comma switch
        {
            0 => value.ToString(),
            >= 1 and <= 4 => value.ToString("F" + comma),
            7 => value.ToString(),
            _ => throw new NotSupportedException()
        };
    }

    public float ParseS16C(ReadOnlySpan<byte> bytes, byte comma)
    {
        var value = BinaryPrimitives.ReadInt16LittleEndian(bytes);
        var commaMultiplier = GetComma(comma);
        return value * commaMultiplier;
    }

    public string StringParseS16C(ReadOnlySpan<byte> bytes, byte comma)
    {
        return FloatToString(ParseS16C(bytes, comma), comma);
    }

    public float ParseU16C(ReadOnlySpan<byte> bytes, byte comma)
    {
        var value = BinaryPrimitives.ReadUInt16LittleEndian(bytes);
        var commaMultiplier = GetComma(comma);
        return value * commaMultiplier;
    }

    public string StringParseU16C(ReadOnlySpan<byte> bytes, byte comma)
    {
        return FloatToString(ParseU16C(bytes, comma), comma);
    }

    public float ParseFS16C(ReadOnlySpan<byte> bytes, byte comma)
    {
        var value = BinaryPrimitives.ReadUInt16LittleEndian(bytes);
        var mantissa = value & 0x3FFF;
        var sign = ((value >> 14) & 1) == 0 ? 1 : -1;
        var exponent = value >> 15;
        var commaMultiplier = GetComma(comma);
        return mantissa * sign * MathF.Pow(10, exponent) * commaMultiplier;
    }

    public string StringParseFS16C(ReadOnlySpan<byte> bytes, byte comma)
    {
        return FloatToString(ParseFS16C(bytes, comma), comma);
    }

    public float ParseFU16C(ReadOnlySpan<byte> bytes, byte comma)
    {
        var value = BinaryPrimitives.ReadUInt16LittleEndian(bytes);
        var mantissa = value & 0x3FFF;
        var exponent = value >> 14;
        var commaMultiplier = GetComma(comma);
        return mantissa * MathF.Pow(10, exponent) * commaMultiplier;
    }

    public string StringParseFU16C(ReadOnlySpan<byte> bytes, byte comma)
    {
        return FloatToString(ParseFU16C(bytes, comma), comma);
    }

    public float ParseS32C(ReadOnlySpan<byte> bytes, byte comma, byte trim)
    {
        var value = BinaryPrimitives.ReadInt32LittleEndian(bytes);
        var commaMultiplier = GetComma(comma);
        return (value * commaMultiplier).TrimDecimalPlaces(trim);
    }

    public string StringParseS32C(ReadOnlySpan<byte> bytes, byte comma, byte trim)
    {
        return FloatToString(ParseS32C(bytes, comma, trim), comma);
    }

    public float ParseS48C(ReadOnlySpan<byte> bytes, byte comma, byte trim)
    {
        var value = BinaryPrimitives.ReadInt64LittleEndian(bytes);
        var commaMultiplier = GetComma(comma);
        return (value * commaMultiplier).TrimDecimalPlaces(trim);
    }

    public string StringParseS48C(ReadOnlySpan<byte> bytes, byte comma, byte trim)
    {
        return FloatToString(ParseS48C(bytes, comma, trim), comma);
    }

    public string StringParseSeconds(ReadOnlySpan<byte> bytes)
    {
        var timeSpan = ParseSeconds(bytes);
        // Using the custom format "hhhh:mm:ss" for hours, minutes, and seconds
        return $"{timeSpan.Days * 24 + timeSpan.Hours}:{timeSpan.Minutes:D2}:{timeSpan.Seconds:D2}";
    }

    public TimeSpan ParseSeconds(ReadOnlySpan<byte> bytes)
    {
        return TimeSpan.FromSeconds(BinaryPrimitives.ReadUInt32LittleEndian(bytes));
    }

    public DateTime ParseDateTime(ReadOnlySpan<byte> bytes)
    {
        return new(2000 + bytes[5], bytes[4], bytes[3], bytes[2], bytes[1], bytes[0]);
    }

    public string StringParseDateTime(ReadOnlySpan<byte> bytes)
    {
        return ParseDateTime(bytes).ToString("ss:mm:HH dd.MM.yy");
    }
}
