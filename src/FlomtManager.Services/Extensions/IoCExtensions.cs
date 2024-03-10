using FlomtManager.Core.Services;
using FlomtManager.Services.Services;
using Microsoft.Extensions.DependencyInjection;

namespace FlomtManager.Services.Extensions
{
    public static class IoCExtensions
    {
        public static IServiceCollection AddServices(this IServiceCollection services)
        {
            // Register services
            services.Add(new ServiceDescriptor(typeof(IDataService), typeof(DataService), ServiceLifetime.Transient));
            services.Add(new ServiceDescriptor(typeof(IModbusService), typeof(ModbusService), ServiceLifetime.Transient));

            return services;
        }
    }
}
