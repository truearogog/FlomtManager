using FlomtManager.Core.Models;

namespace FlomtManager.Core.Events;

public class DeviceConnectionDataChangedArgs : EventArgs
{
    public required IReadOnlyDictionary<byte, ParameterValue> CurrentParameterValues { get; init; }
    public required IReadOnlyDictionary<byte, ParameterValue> IntegralParameterValues { get; init; }
}
