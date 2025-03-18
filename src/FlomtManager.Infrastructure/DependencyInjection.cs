using FlomtManager.Domain.Abstractions.Data;
using FlomtManager.Domain.Abstractions.Providers;
using FlomtManager.Domain.Abstractions.Repositories;
using FlomtManager.Domain.Abstractions.Stores;
using FlomtManager.Infrastructure.Data;
using FlomtManager.Infrastructure.Providers;
using FlomtManager.Infrastructure.Repositories;
using FlomtManager.Infrastructure.Stores;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace FlomtManager.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AppDatabase(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("AppDb") ?? throw new Exception("Database connection string is null");

        services.AddSingleton<IDbConnectionFactory, DbConnectionFactory>(sp => new DbConnectionFactory(connectionString));
        services.AddSingleton<IDbInitializer, DbInitializer>();

        return services;
    }

    public static IServiceCollection AddRepositories(this IServiceCollection services)
    {
        services.AddTransient<IDataRepository, DataRepository>();
        services.AddTransient<IParameterRepository, ParameterRepository>();
        services.AddTransient<IDeviceRepository, DeviceRepository>();

        return services;
    }

    public static IServiceCollection AddServices(this IServiceCollection services)
    {
        services.AddSingleton<IDateTimeProvider, DateTimeProvider>();

        return services;
    }

    public static IServiceCollection AddStores(this IServiceCollection services)
    {
        services.AddSingleton<IDeviceStore, DeviceStore>();

        return services;
    }
}
