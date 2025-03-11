using System.Buffers.Binary;
using System.Collections.ObjectModel;
using System.Text;
using FlomtManager.Core.Constants;
using FlomtManager.Core.Enums;
using FlomtManager.Core.Models;
using FlomtManager.Core.Parsers;
using FlomtManager.Framework.Skia;

namespace FlomtManager.Application.Parsers;

internal sealed class DataParser(IDataFormatter dataFormatter) : IDataParser
{
    /*
    200.352 [C] "All parameters definition" 22 parameters * 16 one block length)
        X... Description of 1 parameter Bytes: (16 bytes per parameter)
        0 - Assigning a number to the parameter 0 - disabled
                this number will be used when describing parameter blocks
        1 - Parameter type *** 
        2,3 - UINT16 error mask
        4 - Assign parameter number after integration (0 - ordinary parameter) 
                for example: Q(m3/h)(INT16) after integration converted to V(m3)(INT32)
        5 - reserved 
        6...9 -'Q',0,0,0 - symbolic name of the parameter - 4 characters max or up to 0
        10...15 - 'm3/h',0,0 - parameter units - 6 characters max or up to 0
    */
    public ReadOnlyCollection<Parameter> ParseParameterDefinition(ReadOnlySpan<byte> bytes, int deviceId)
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
                    DeviceId = deviceId,
                    Number = parameterBytes[0],
                    Type = type,
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
            01000.XXX(64-71) - S48C0...S48C7     - 64(48) bit value fixed comma(used 48 bits)
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
    public (ParameterType Type, byte Comma) ParseParameterTypeByte(byte parameterTypeByte) => parameterTypeByte switch
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
        96 => (ParameterType.Error, 0),
        97 => (ParameterType.WorkingTimeInSeconds, 0),
        98 => (ParameterType.WorkingTimeInSecondsInArchiveInterval, 0),
        99 => (ParameterType.WorkingTimeInMinutesInArchiveInterval, 0),
        100 => (ParameterType.WorkingTimeInHoursInArchiveInterval, 0),
        101 => (ParameterType.Time, 0),
        102 => (ParameterType.SecondsSince2000, 0),
        _ => throw new NotSupportedException(),
    };

    public string ParseBytesToString(ReadOnlySpan<byte> bytes, Parameter parameter) => parameter.Type switch
    {
        ParameterType.S16C => StringParseS16C(bytes, parameter.Comma),
        ParameterType.U16C => StringParseU16C(bytes, parameter.Comma),
        ParameterType.FS16C => StringParseFS16C(bytes, parameter.Comma),
        ParameterType.FU16C => StringParseFU16C(bytes, parameter.Comma),
        ParameterType.S32C => StringParseS32CD(bytes, 0, parameter.Comma),
        ParameterType.S32CD1 => StringParseS32CD(bytes, 1, parameter.Comma),
        ParameterType.S32CD2 => StringParseS32CD(bytes, 2, parameter.Comma),
        ParameterType.S32CD3 => StringParseS32CD(bytes, 3, parameter.Comma),
        ParameterType.S48C => StringParseS48CD(bytes, 0, parameter.Comma),
        ParameterType.S48CD1 => StringParseS48CD(bytes, 1, parameter.Comma),
        ParameterType.S48CD2 => StringParseS48CD(bytes, 2, parameter.Comma),
        ParameterType.S48CD3 => StringParseS48CD(bytes, 3, parameter.Comma),
        ParameterType.Error => StringParseUInt16(bytes),
        ParameterType.WorkingTimeInSeconds => StringParseWorkingTimeInSeconds(bytes),
        ParameterType.WorkingTimeInSecondsInArchiveInterval => StringParseUInt16(bytes),
        ParameterType.WorkingTimeInMinutesInArchiveInterval => StringParseUInt16(bytes),
        ParameterType.WorkingTimeInHoursInArchiveInterval => StringParseUInt16(bytes),
        ParameterType.Time => StringParseDateTime(bytes),
        ParameterType.SecondsSince2000 => StringParseUInt32(bytes),
        _ => string.Empty,
    };

    public float ParseBytesToFloat(ReadOnlySpan<byte> bytes, Parameter parameter) => parameter.Type switch
    {
        ParameterType.S16C => ParseS16C(bytes, parameter.Comma),
        ParameterType.U16C => ParseU16C(bytes, parameter.Comma),
        ParameterType.FS16C => ParseFS16C(bytes, parameter.Comma),
        ParameterType.FU16C => ParseFU16C(bytes, parameter.Comma),
        ParameterType.S32C => ParseS32CD(bytes, 0, parameter.Comma),
        ParameterType.S32CD1 => ParseS32CD(bytes, 1, parameter.Comma),
        ParameterType.S32CD2 => ParseS32CD(bytes, 2, parameter.Comma),
        ParameterType.S32CD3 => ParseS32CD(bytes, 3, parameter.Comma),
        ParameterType.S48C => ParseS48CD(bytes, 0, parameter.Comma),
        ParameterType.S48CD1 => ParseS48CD(bytes, 1, parameter.Comma),
        ParameterType.S48CD2 => ParseS48CD(bytes, 2, parameter.Comma),
        ParameterType.S48CD3 => ParseS48CD(bytes, 3, parameter.Comma),
        _ => 0
    };

    public uint ParseBytesToUInt32(ReadOnlySpan<byte> bytes, Parameter parameter) => parameter.Type switch
    {
        ParameterType.SecondsSince2000 => ParseUInt32(bytes),
        _ => 0
    };

    public ushort ParseBytesToUInt16(ReadOnlySpan<byte> bytes, Parameter parameter) => parameter.Type switch
    {
        ParameterType.Error => ParseUInt16(bytes),
        ParameterType.WorkingTimeInSecondsInArchiveInterval => ParseUInt16(bytes),
        ParameterType.WorkingTimeInMinutesInArchiveInterval => ParseUInt16(bytes),
        ParameterType.WorkingTimeInHoursInArchiveInterval => ParseUInt16(bytes),
        _ => 0
    };

    public TimeSpan ParseBytesToTimeSpan(ReadOnlySpan<byte> bytes, Parameter parameter) => parameter.Type switch
    {
        ParameterType.WorkingTimeInSeconds => ParseWorkingTimeInSeconds(bytes),
        _ => TimeSpan.FromTicks(0),
    };

    public DateTime ParseBytesToDateTime(ReadOnlySpan<byte> bytes, Parameter parameter) => parameter.Type switch
    {
        ParameterType.Time => ParseDateTime(bytes),
        _ => DateTime.MinValue,
    };

    #region S16C

    private string StringParseS16C(ReadOnlySpan<byte> bytes, byte comma) => dataFormatter.FormatFloat(ParseS16C(bytes, comma), 0, comma);

    private static float ParseS16C(ReadOnlySpan<byte> bytes, byte comma)
    {
        var value = BinaryPrimitives.ReadInt16LittleEndian(bytes);
        return Round(value, comma);
    }

    #endregion

    #region U16C

    private string StringParseU16C(ReadOnlySpan<byte> bytes, byte comma) => dataFormatter.FormatFloat(ParseS16C(bytes, comma), 0, comma);

    private static float ParseU16C(ReadOnlySpan<byte> bytes, byte comma)
    {
        var value = BinaryPrimitives.ReadUInt16LittleEndian(bytes);
        return Round(value, comma);
    }

    #endregion

    #region FS16C

    private string StringParseFS16C(ReadOnlySpan<byte> bytes, byte comma) => dataFormatter.FormatFloat(ParseFS16C(bytes, comma), 0, comma);

    private static float ParseFS16C(ReadOnlySpan<byte> bytes, byte comma)
    {
        var value = BinaryPrimitives.ReadUInt16LittleEndian(bytes);
        var mantissa = value & 0x3FFF;
        var sign = ((value >> 14) & 1) == 0 ? 1 : -1;
        var exponent = value >> 15;
        var multiplier = MathF.Pow(10, exponent);
        return Round(mantissa * sign * multiplier, comma);
    }

    #endregion

    #region FU16C

    private string StringParseFU16C(ReadOnlySpan<byte> bytes, byte comma) => dataFormatter.FormatFloat(ParseFU16C(bytes, comma), 0, comma);

    private static float ParseFU16C(ReadOnlySpan<byte> bytes, byte comma)
    {
        var value = BinaryPrimitives.ReadUInt16LittleEndian(bytes);
        var mantissa = value & 0x3FFF;
        var exponent = value >> 14;
        var multiplier = MathF.Pow(10, exponent);
        return Round(mantissa * multiplier, comma);
    }

    #endregion

    #region S32CD

    private string StringParseS32CD(ReadOnlySpan<byte> bytes, byte trim, byte comma) => dataFormatter.FormatFloat(ParseS32CD(bytes, trim, comma), trim, comma);

    private static float ParseS32CD(ReadOnlySpan<byte> bytes, byte trim, byte comma)
    {
        var value = BinaryPrimitives.ReadInt32LittleEndian(bytes);
        return Round(value, trim, comma);
    }

    #endregion

    #region S48CD

    private string StringParseS48CD(ReadOnlySpan<byte> bytes, byte trim, byte comma) => dataFormatter.FormatFloat(ParseS48CD(bytes, trim, comma), trim, comma);

    private static float ParseS48CD(ReadOnlySpan<byte> bytes, byte trim, byte comma)
    {
        var value = BinaryPrimitives.ReadInt64LittleEndian(bytes);
        return Round(value, trim, comma);
    }

    #endregion

    #region WorkingTimeInSeconds

    private string StringParseWorkingTimeInSeconds(ReadOnlySpan<byte> bytes) => dataFormatter.FormatTimeSpan(ParseWorkingTimeInSeconds(bytes));

    private static TimeSpan ParseWorkingTimeInSeconds(ReadOnlySpan<byte> bytes) => TimeSpan.FromSeconds(BinaryPrimitives.ReadUInt32LittleEndian(bytes));

    #endregion

    #region UInt16

    private string StringParseUInt16(ReadOnlySpan<byte> bytes) => dataFormatter.FormatUInt16(ParseUInt16(bytes));

    public ushort ParseUInt16(ReadOnlySpan<byte> bytes) => BitConverter.ToUInt16(bytes);

    #endregion

    #region UInt32

    private string StringParseUInt32(ReadOnlySpan<byte> bytes) => dataFormatter.FormatUInt32(ParseUInt32(bytes));

    private static uint ParseUInt32(ReadOnlySpan<byte> bytes) => BitConverter.ToUInt32(bytes);

    #endregion

    #region DateTime

    private string StringParseDateTime(ReadOnlySpan<byte> bytes) => dataFormatter.FormatDateTime(ParseDateTime(bytes));

    public DateTime ParseDateTime(ReadOnlySpan<byte> bytes) => new(2000 + bytes[5], bytes[4], bytes[3], bytes[2], bytes[1], bytes[0]);

    #endregion

    private static float Round(float value, byte comma)
    {
        value *= _commaMultipliers[(byte)(comma & 0b111)];
        return float.Round(value, _commaPositions[comma]);
    }

    private static float Round(float value, byte trim, byte comma)
    {
        value = (int)(value * _trimFractions[trim]) * _trimMultipliers[trim] * _commaMultipliers[(byte)(comma & 0b111)];
        return float.Round(value, _commaPositions[comma]);
    }

    private static byte GetCommaFromByte(byte commaByte) => commaByte &= 0b111;

    private static readonly Dictionary<byte, float> _trimFractions = new()
    {
        [0] = 1f,
        [1] = 0.1f,
        [2] = 0.01f,
        [3] = 0.001f
    };

    private static readonly Dictionary<byte, float> _trimMultipliers = new()
    {
        [0] = 1f,
        [1] = 10f,
        [2] = 100f,
        [3] = 1000f
    };

    private static readonly Dictionary<byte, float> _commaMultipliers = new()
    {
        [0] = 1f,
        [1] = 0.1f,
        [2] = 0.01f,
        [3] = 0.001f,
        [4] = 0.0001f,
        [7] = 10f
    };

    private static readonly Dictionary<byte, short> _commaPositions = new()
    {
        [0] = 0,
        [1] = 1,
        [2] = 2,
        [3] = 3,
        [4] = 4,
        [7] = 0
    };
}
