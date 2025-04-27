using FlomtManager.Domain.Enums;

namespace FlomtManager.Domain.Models;

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

    bool IsEnabled,
    bool IsAxisVisibleOnChart,
    bool IsAutoScaledOnChart,
    double ZoomLevelOnChart);