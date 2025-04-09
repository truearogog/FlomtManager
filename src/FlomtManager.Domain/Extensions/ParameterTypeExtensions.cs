using FlomtManager.Domain.Constants;
using FlomtManager.Domain.Enums;

namespace FlomtManager.Domain.Extensions;

public static class ParameterTypeExtensions
{
    public static Type GetClrType(this ParameterType type) => type switch
    {
        ParameterType.S16C => typeof(float),
        ParameterType.U16C => typeof(float),
        ParameterType.FS16C => typeof(float),
        ParameterType.FU16C => typeof(float),
        ParameterType.S32C => typeof(float),
        ParameterType.S32CD1 => typeof(float),
        ParameterType.S32CD2 => typeof(float),
        ParameterType.S32CD3 => typeof(float),
        ParameterType.S48C => typeof(float),
        ParameterType.S48CD1 => typeof(float),
        ParameterType.S48CD2 => typeof(float),
        ParameterType.S48CD3 => typeof(float),
        ParameterType.Error => typeof(ushort),
        ParameterType.WorkingTimeInSeconds => typeof(TimeSpan),
        ParameterType.WorkingTimeInSecondsInArchiveInterval => typeof(ushort),
        ParameterType.WorkingTimeInMinutesInArchiveInterval => typeof(ushort),
        ParameterType.WorkingTimeInHoursInArchiveInterval => typeof(ushort),
        ParameterType.Time => typeof(DateTime),
        ParameterType.SecondsSince2000 => typeof(uint),
        _ => throw new NotSupportedException()
    };

    public static string GetSqlType(this ParameterType type) => type switch
    {
        ParameterType.S16C => SqlTypes.Real,
        ParameterType.U16C => SqlTypes.Real,
        ParameterType.FS16C => SqlTypes.Real,
        ParameterType.FU16C => SqlTypes.Real,
        ParameterType.S32C => SqlTypes.Real,
        ParameterType.S32CD1 => SqlTypes.Real,
        ParameterType.S32CD2 => SqlTypes.Real,
        ParameterType.S32CD3 => SqlTypes.Real,
        ParameterType.S48C => SqlTypes.Real,
        ParameterType.S48CD1 => SqlTypes.Real,
        ParameterType.S48CD2 => SqlTypes.Real,
        ParameterType.S48CD3 => SqlTypes.Real,
        ParameterType.Error => SqlTypes.Int,
        ParameterType.WorkingTimeInSeconds => SqlTypes.TimeSpan,
        ParameterType.WorkingTimeInSecondsInArchiveInterval => SqlTypes.Int,
        ParameterType.WorkingTimeInMinutesInArchiveInterval => SqlTypes.Int,
        ParameterType.WorkingTimeInHoursInArchiveInterval => SqlTypes.Int,
        ParameterType.Time => SqlTypes.DateTime,
        ParameterType.SecondsSince2000 => SqlTypes.Int,
        _ => throw new NotSupportedException()
    };

    public static bool HideType(this ParameterType type) => type switch
    {
        ParameterType.Error => true,
        ParameterType.WorkingTimeInSecondsInArchiveInterval => false,
        ParameterType.WorkingTimeInMinutesInArchiveInterval => true,
        ParameterType.WorkingTimeInHoursInArchiveInterval => true,
        ParameterType.Time => true,
        ParameterType.SecondsSince2000 => true,
        _ => false
    };

    public static byte GetSize(this ParameterType type) => type switch
    {
        ParameterType.S16C => 2,
        ParameterType.U16C => 2,
        ParameterType.FS16C => 2,
        ParameterType.FU16C => 2,
        ParameterType.S32C => 4,
        ParameterType.S32CD1 => 4,
        ParameterType.S32CD2 => 4,
        ParameterType.S32CD3 => 4,
        ParameterType.S48C => 8,
        ParameterType.S48CD1 => 8,
        ParameterType.S48CD2 => 8,
        ParameterType.S48CD3 => 8,
        ParameterType.Error => 2,
        ParameterType.WorkingTimeInSeconds => 4,
        ParameterType.WorkingTimeInSecondsInArchiveInterval => 2,
        ParameterType.WorkingTimeInMinutesInArchiveInterval => 2,
        ParameterType.WorkingTimeInHoursInArchiveInterval => 2,
        ParameterType.Time => 1,
        ParameterType.SecondsSince2000 => 4,
        _ => throw new NotSupportedException()
    };
}
