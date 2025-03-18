using FlomtManager.Domain.Abstractions.DeviceConnection;
using FlomtManager.Domain.Abstractions.Parsers;
using FlomtManager.Domain.Abstractions.Repositories;
using FlomtManager.Domain.Abstractions.Stores;
using FlomtManager.Domain.Models;
using Microsoft.Extensions.DependencyInjection;

namespace FlomtManager.Application.DeviceConnection;

internal sealed class FileDataImporterFactory(IServiceProvider serviceProvider) : IFileDataImporterFactory
{
    public IFileDataImporter Create(Device device)
    {
        var deviceStore = serviceProvider.GetRequiredService<IDeviceStore>();
        var dataParser = serviceProvider.GetRequiredService<IDataParser>();
        var dataRepository = serviceProvider.GetRequiredService<IDataRepository>();
        var deviceRepository = serviceProvider.GetRequiredService<IDeviceRepository>();
        var parameterRepository = serviceProvider.GetRequiredService<IParameterRepository>();

        return new FileDataImporter(device, deviceStore, dataParser, dataRepository, deviceRepository, parameterRepository);
    }
}
