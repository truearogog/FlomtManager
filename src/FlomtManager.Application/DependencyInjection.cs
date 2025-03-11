using FlomtManager.Application.DeviceConnection;
using FlomtManager.Application.Parsers;
using FlomtManager.Core.Parsers;
using FlomtManager.Core.ViewModels;
using Microsoft.Extensions.DependencyInjection;

namespace FlomtManager.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddSingleton<IDeviceConnectionFactory, DeviceConnectionFactory>();
        services.AddSingleton<IDataParser, DataParser>();
        services.AddSingleton<IDataFormatter, DataFormatter>();

        return services;
    }
}
