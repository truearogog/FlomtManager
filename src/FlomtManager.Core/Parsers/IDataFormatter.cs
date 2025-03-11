using FlomtManager.Core.Models;

namespace FlomtManager.Core.Parsers;

public interface IDataFormatter
{
    string FormatFloat(float value, Parameter parameter);
    string FormatFloat(float value, byte trim, byte comma);
    string FormatUInt32(uint value);
    string FormatUInt16(ushort value);
    string FormatTimeSpan(TimeSpan value);
    string FormatDateTime(DateTime value);

    string GetParameterFormat(Parameter type);
}
