using System;
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
        ParameterType.S16C      => FormatFloat(value, 0, parameter.Comma),
        ParameterType.U16C      => FormatFloat(value, 0, parameter.Comma),
        ParameterType.FS16C     => FormatFloat(value, 0, parameter.Comma),
        ParameterType.FU16C     => FormatFloat(value, 0, parameter.Comma),
        ParameterType.S32C      => FormatFloat(value, 0, parameter.Comma),
        ParameterType.S32CD1    => FormatFloat(value, 1, parameter.Comma),
        ParameterType.S32CD2    => FormatFloat(value, 2, parameter.Comma),
        ParameterType.S32CD3    => FormatFloat(value, 3, parameter.Comma),
        ParameterType.S48C      => FormatFloat(value, 0, parameter.Comma),
        ParameterType.S48CD1    => FormatFloat(value, 1, parameter.Comma),
        ParameterType.S48CD2    => FormatFloat(value, 2, parameter.Comma),
        ParameterType.S48CD3    => FormatFloat(value, 3, parameter.Comma),
        _ => string.Empty
    };

    public string FormatFloat(float value, byte trim, byte comma)
    {
        var commaPosition = (short)Math.Max(0, _commaPositions[(byte)(comma & 0b111)] - trim);
        return value.ToString(_commaFormats[commaPosition]);
    }

    public string FormatUInt32(uint value) => value.ToString();

    public string FormatUInt16(ushort value) => value.ToString();

    // Using the custom format "hhhh:mm:ss" for hours, minutes, and seconds
    public string FormatTimeSpan(TimeSpan value) => $"{value.Days * 24 + value.Hours}:{value.Minutes:D2}:{value.Seconds:D2}";

    public string FormatDateTime(DateTime value) => value.ToString("yy.MM.dd HH:mm:ss");
}
