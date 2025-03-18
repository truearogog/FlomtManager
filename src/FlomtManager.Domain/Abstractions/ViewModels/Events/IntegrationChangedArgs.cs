namespace FlomtManager.Domain.Abstractions.ViewModels.Events;

public class IntegrationChangedArgs : EventArgs
{
    public required DateTime IntegrationStart { get; init; }
    public required DateTime IntegrationEnd { get; init; }
    public required IReadOnlyDictionary<byte, string> IntegratedValues { get; init; }
}
