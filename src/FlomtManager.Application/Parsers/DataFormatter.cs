using FlomtManager.Core.Enums;
using FlomtManager.Core.Models;
using FlomtManager.Core.Parsers;

namespace FlomtManager.Application.Parsers;

internal sealed class DataFormatter : IDataFormatter
{
    private static readonly Dictionary<byte, short> _commaPositions = new()
    {
        [0] = 0,
        [1] = 1,
        [2] = 2,
        [3] = 3,
        [4] = 4,
        [7] = 0
    };

    private static readonly Dictionary<short, string> _commaFormats =
        Enumerable.Range(0, 10).ToDictionary(x => (short)x, x => x switch
        {
            0 => "0",
            _ => "0." + new string(Enumerable.Repeat('0', x).ToArray()),
        });

    public string FormatFloat(float value, Parameter parameter) => parameter.Type switch
    {
        ParameterType.S16C => FormatFloat(value, 0, parameter.Comma),
        ParameterType.U16C => FormatFloat(value, 0, parameter.Comma),
        ParameterType.FS16C => FormatFloat(value, 0, parameter.Comma),
        ParameterType.FU16C => FormatFloat(value, 0, parameter.Comma),
        ParameterType.S32C => FormatFloat(value, 0, parameter.Comma),
        ParameterType.S32CD1 => FormatFloat(value, 1, parameter.Comma),
        ParameterType.S32CD2 => FormatFloat(value, 2, parameter.Comma),
        ParameterType.S32CD3 => FormatFloat(value, 3, parameter.Comma),
        ParameterType.S48C => FormatFloat(value, 0, parameter.Comma),
        ParameterType.S48CD1 => FormatFloat(value, 1, parameter.Comma),
        ParameterType.S48CD2 => FormatFloat(value, 2, parameter.Comma),
        ParameterType.S48CD3 => FormatFloat(value, 3, parameter.Comma),
        _ => string.Empty
    };

    public string FormatFloat(float value, byte trim, byte comma) => value.ToString(GetFloatFormat(trim, comma));

    public string FormatUInt32(uint value) => value.ToString(GetUInt32Format());

    public string FormatUInt16(ushort value) => value.ToString(GetUInt16Format());

    // Using the custom format "hhhh:mm:ss" for hours, minutes, and seconds
    public string FormatTimeSpan(TimeSpan value) => $"{value.Days * 24 + value.Hours}:{value.Minutes:D2}:{value.Seconds:D2}";

    public string FormatDateTime(DateTime value) => value.ToString(GetDateTimeFormat());

    public string GetParameterFormat(Parameter parameter) => parameter.Type switch
    {
        ParameterType.S16C => GetFloatFormat(0, parameter.Comma),
        ParameterType.U16C => GetFloatFormat(0, parameter.Comma),
        ParameterType.FS16C => GetFloatFormat(0, parameter.Comma),
        ParameterType.FU16C => GetFloatFormat(0, parameter.Comma),
        ParameterType.S32C => GetFloatFormat(0, parameter.Comma),
        ParameterType.S32CD1 => GetFloatFormat(1, parameter.Comma),
        ParameterType.S32CD2 => GetFloatFormat(2, parameter.Comma),
        ParameterType.S32CD3 => GetFloatFormat(3, parameter.Comma),
        ParameterType.S48C => GetFloatFormat(0, parameter.Comma),
        ParameterType.S48CD1 => GetFloatFormat(1, parameter.Comma),
        ParameterType.S48CD2 => GetFloatFormat(2, parameter.Comma),
        ParameterType.S48CD3 => GetFloatFormat(3, parameter.Comma),
        ParameterType.Error => GetUInt16Format(),
        ParameterType.WorkingTimeInSeconds => "{0}",
        ParameterType.WorkingTimeInSecondsInArchiveInterval => GetUInt16Format(),
        ParameterType.WorkingTimeInMinutesInArchiveInterval => GetUInt16Format(),
        ParameterType.WorkingTimeInHoursInArchiveInterval => GetUInt16Format(),
        ParameterType.Time => GetDateTimeFormat(),
        ParameterType.SecondsSince2000 => GetUInt32Format(),
        _ => "{0}",
    };

    private static string GetFloatFormat(byte trim, byte comma)
    {
        var commaPosition = (short)Math.Max(0, _commaPositions[(byte)(comma & 0b111)] - trim);
        return _commaFormats[commaPosition];
    }

    private static string GetUInt32Format() => "{0}";

    private static string GetUInt16Format() => "{0}";

    private static string GetDateTimeFormat() => "yy.MM.dd HH:mm:ss";
}
