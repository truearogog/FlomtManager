namespace FlomtManager.Domain.Abstractions.Data;

public interface IDbInitializer
{
    Task Init(CancellationToken cancellationToken = default);
    Task Drop(CancellationToken cancellationToken = default);
}
