using EFCore.BulkExtensions;
using FlomtManager.Data.EF.Entities;
using Microsoft.EntityFrameworkCore;

namespace FlomtManager.Data.EF
{
    public abstract class AppDb<TDb>(DbContextOptions<TDb> options) : DbContext(options), IAppDb where TDb : AppDb<TDb>
    {
        public DbSet<DeviceEntity> Devices { get; set; }
        public DbSet<DeviceDefinitionEntity> DeviceDefinitions { get; set; }
        public DbSet<ParameterEntity> Parameters { get; set; }

        public async Task InsertRangeAsync<T>(IEnumerable<T> entities, BulkConfig bulkConfig = null,
            Action<decimal> progress = null, Type type = null, CancellationToken cancellationToken = default) where T : class
        {
            await this.BulkInsertAsync(entities, bulkConfig, progress, type, cancellationToken);
        }
        public async Task InsertOrUpdateRangeAsync<T>(IEnumerable<T> entities, BulkConfig bulkConfig = null,
            Action<decimal> progress = null, Type type = null, CancellationToken cancellationToken = default) where T : class
        {
            await this.BulkInsertOrUpdateAsync(entities, bulkConfig, progress, type, cancellationToken);
        }
        public async Task UpdateRangeAsync<T>(IEnumerable<T> entities, BulkConfig bulkConfig = null,
            Action<decimal> progress = null, Type type = null) where T : class
        {
            await this.BulkUpdateAsync(entities, bulkConfig, progress, type);
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<DeviceEntity>().HasMany(x => x.Parameters).WithOne(x => x.Device);
            modelBuilder.Entity<DeviceEntity>().HasOne(x => x.DeviceDefinition).WithOne(x => x.Device).HasForeignKey<DeviceDefinitionEntity>(x => x.DeviceId).IsRequired(false);

            modelBuilder.Entity<DeviceDefinitionEntity>().HasOne(x => x.Device).WithOne(x => x.DeviceDefinition);

            modelBuilder.Entity<ParameterEntity>().HasOne(x => x.Device).WithMany(x => x.Parameters);
        }
    }
}
