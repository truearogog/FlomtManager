using FlomtManager.Core.Enums;

namespace FlomtManager.Core.Models;

public readonly record struct Parameter(
    int Id,
    int DeviceId,

    DateTime Created,
    DateTime Updated,

    byte Number,
    ParameterType Type,
    byte Comma,
    ushort ErrorMask,
    byte IntegrationNumber,
    string Name,
    string Unit,
    string Color,

    ChartScalingType ChartYScalingType,
    double ChartYZoom);