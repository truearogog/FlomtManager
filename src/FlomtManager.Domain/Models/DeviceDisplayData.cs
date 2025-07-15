namespace FlomtManager.Domain.Models;

public readonly record struct DeviceDisplayData(
    Dictionary<byte, string> IntegralParameters,
    Dictionary<byte, string> CurrentParameters,
    double IntegrationStart,
    double IntegrationEnd,
    double MinX,
    double MaxX,
    double YAxisZoom);
