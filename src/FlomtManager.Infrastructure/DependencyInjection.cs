using FlomtManager.Core;
using FlomtManager.Core.Repositories;
using FlomtManager.Core.Services;
using FlomtManager.Infrastructure.Repositories;
using FlomtManager.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace FlomtManager.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AppAppEF(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("AppDb") ?? throw new Exception("Database connection string is null");

        services.AddDbContext<IAppDb, AppDb>(options =>
        {
            options.UseSqlite(connectionString).EnableSensitiveDataLogging();
        });

        services.AddTransient<IDataGroupRepository, DataGroupRepository>();
        services.AddTransient<IDeviceDefinitionRepository, DeviceDefinitionRepository>();
        services.AddTransient<IDeviceRepository, DeviceRepository>();
        services.AddTransient<IParameterRepository, ParameterRepository>();

        return services;
    }

    public static IServiceCollection AddServices(this IServiceCollection services)
    {
        services.AddTransient<IDataService, DataService>();
        services.AddTransient<IModbusService, ModbusService>();

        return services;
    }
}
