using FlomtManager.Application.DeviceConnection;
using FlomtManager.Application.Parsers;
using FlomtManager.Application.ViewModelFactories;
using FlomtManager.Application.ViewModels;
using FlomtManager.Domain.Abstractions.DeviceConnection;
using FlomtManager.Domain.Abstractions.Parsers;
using FlomtManager.Domain.Abstractions.ViewModelFactories;
using FlomtManager.Domain.Abstractions.ViewModels;
using Microsoft.Extensions.DependencyInjection;

namespace FlomtManager.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services
            .AddDeviceConnection()
            .AddDataParsers()
            .AddViewModels();

        return services;
    }

    private static IServiceCollection AddDeviceConnection(this IServiceCollection services)
    {
        services.AddSingleton<IDeviceConnectionFactory, DeviceConnectionFactory>();
        services.AddSingleton<IFileDataImporterFactory, FileDataImporterFactory>();

        return services;
    }

    private static IServiceCollection AddDataParsers(this IServiceCollection services)
    {
        services.AddSingleton<IDataParser, DataParser>();
        services.AddSingleton<IDataFormatter, DataFormatter>();

        return services;
    }

    private static IServiceCollection AddViewModels(this IServiceCollection services)
    {
        services.AddTransient<IAboutViewModel, AboutViewModel>();
        services.AddTransient<IDevicesViewModel, DevicesViewModel>();
        services.AddTransient<IDeviceCreateUpdateViewModel, DeviceCreateUpdateViewModel>();
        services.AddTransient<IDeviceDeleteViewModel, DeviceDeleteViewModel>();
        services.AddTransient<IDeviceViewModel, DeviceViewModel>();
        services.AddTransient<IDataChartViewModel, DataChartViewModel>();
        services.AddTransient<IDataTableViewModel, DataTableViewModel>();
        services.AddTransient<IDataIntegrationViewModel, DataIntegrationViewModel>();

        services.AddTransient<IDeviceFormViewModelFactory, DeviceFormViewModelFactory>();
        services.AddTransient<IParameterViewModelFactory, ParameterViewModelFactory>();

        return services;
    }
}
