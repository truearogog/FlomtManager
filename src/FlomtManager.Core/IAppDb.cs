using FlomtManager.Core.Entities;
using Microsoft.EntityFrameworkCore;

namespace FlomtManager.Core;

public interface IAppDb
{
    DbSet<Device> Devices { get; set; }
    DbSet<DeviceDefinition> DeviceDefinitions { get; set; }
    DbSet<Parameter> Parameters { get; set; }
    DbSet<DataGroup> DataGroups { get; set; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}