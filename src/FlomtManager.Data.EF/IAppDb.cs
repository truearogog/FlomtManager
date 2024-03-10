using EFCore.BulkExtensions;
using FlomtManager.Data.EF.Entities;
using Microsoft.EntityFrameworkCore;

namespace FlomtManager.Data.EF
{
    public interface IAppDb
    {
        DbSet<DeviceEntity> Devices { get; set; }
        DbSet<DeviceDefinitionEntity> DeviceDefinitions { get; set; }
        DbSet<ParameterEntity> Parameters { get; set; }
        DbSet<DataGroupEntity> DataGroups { get; set; }

        Task InsertRangeAsync<T>(IEnumerable<T> entities, BulkConfig bulkConfig = null,
            Action<decimal> progress = null, Type type = null, CancellationToken cancellationToken = default) where T : class;
        Task InsertOrUpdateRangeAsync<T>(IEnumerable<T> entities, BulkConfig bulkConfig = null,
            Action<decimal> progress = null, Type type = null, CancellationToken cancellationToken = default) where T : class;
        Task UpdateRangeAsync<T>(IEnumerable<T> entities, BulkConfig bulkConfig = null,
            Action<decimal> progress = null, Type type = null) where T : class;

        int SaveChanges();
        Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
    }
}
