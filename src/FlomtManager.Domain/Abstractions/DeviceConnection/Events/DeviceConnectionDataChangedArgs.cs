using FlomtManager.Domain.Models;

namespace FlomtManager.Domain.Abstractions.DeviceConnection.Events;

public class DeviceConnectionDataChangedArgs : EventArgs
{
    public required IReadOnlyDictionary<byte, ParameterValue> CurrentParameterValues { get; init; }
    public required IReadOnlyDictionary<byte, ParameterValue> IntegralParameterValues { get; init; }
}
