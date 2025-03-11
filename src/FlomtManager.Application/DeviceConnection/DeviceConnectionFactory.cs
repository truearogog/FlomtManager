using FlomtManager.Core.DeviceConnection;
using FlomtManager.Core.Models;
using FlomtManager.Core.Parsers;
using FlomtManager.Core.Repositories;
using FlomtManager.Core.Stores;
using FlomtManager.Core.ViewModels;
using Microsoft.Extensions.DependencyInjection;

namespace FlomtManager.Application.DeviceConnection;

internal sealed class DeviceConnectionFactory(IServiceProvider serviceProvider) : IDeviceConnectionFactory
{
    public IDeviceConnection Create(Device device)
    {
        var deviceStore = serviceProvider.GetRequiredService<IDeviceStore>();
        var dataParser = serviceProvider.GetRequiredService<IDataParser>();
        var dataRepository = serviceProvider.GetRequiredService<IDataRepository>();
        var deviceRepository = serviceProvider.GetRequiredService<IDeviceRepository>();
        var parameterRepository = serviceProvider.GetRequiredService<IParameterRepository>();

        return new DeviceConnection(device, deviceStore, dataParser, dataRepository, deviceRepository, parameterRepository);
    }
}
