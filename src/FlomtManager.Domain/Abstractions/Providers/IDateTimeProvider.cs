namespace FlomtManager.Domain.Abstractions.Providers;

public interface IDateTimeProvider
{
    DateTime Now { get; }
    DateTime UtcNow { get; }
}
