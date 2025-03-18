using FlomtManager.Domain.Models;

namespace FlomtManager.Domain.Abstractions.DeviceConnection;

public interface IFileDataImporterFactory
{
    IFileDataImporter Create(Device device);
}
