﻿using EFCore.BulkExtensions;
using FlomtManager.Data.EF.Entities;
using Microsoft.EntityFrameworkCore;

namespace FlomtManager.Data.EF
{
    public abstract class AppDb<TDb>: DbContext, IAppDb where TDb : AppDb<TDb>
    {
        protected AppDb(DbContextOptions<TDb> options) : base(options)
        {
            //ChangeTracker.AutoDetectChangesEnabled = false;
            //ChangeTracker.LazyLoadingEnabled = false;
            //ChangeTracker.QueryTrackingBehavior = QueryTrackingBehavior.NoTracking;
        }

        public DbSet<DeviceEntity> Devices { get; set; }
        public DbSet<DeviceDefinitionEntity> DeviceDefinitions { get; set; }
        public DbSet<ParameterEntity> Parameters { get; set; }
        public DbSet<DataGroupEntity> DataGroups { get; set; }

        public async Task InsertRangeAsync<T>(IEnumerable<T> entities, BulkConfig? bulkConfig = null,
            Action<decimal>? progress = null, Type? type = null, CancellationToken cancellationToken = default) where T : class
        {
            await this.BulkInsertAsync(entities, bulkConfig, progress, type, cancellationToken);
        }
        public async Task InsertOrUpdateRangeAsync<T>(IEnumerable<T> entities, BulkConfig? bulkConfig = null,
            Action<decimal>? progress = null, Type? type = null, CancellationToken cancellationToken = default) where T : class
        {
            await this.BulkInsertOrUpdateAsync(entities, bulkConfig, progress, type, cancellationToken);
        }
        public async Task UpdateRangeAsync<T>(IEnumerable<T> entities, BulkConfig? bulkConfig = null,
            Action<decimal>? progress = null, Type? type = null) where T : class
        {
            await this.BulkUpdateAsync(entities, bulkConfig, progress, type);
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<DeviceEntity>().HasMany(x => x.Parameters).WithOne(x => x.Device);
            modelBuilder.Entity<DeviceEntity>().HasMany(x => x.DataGroups).WithOne(x => x.Device);
            modelBuilder.Entity<DeviceEntity>().HasOne(x => x.DeviceDefinition).WithOne().HasForeignKey<DeviceDefinitionEntity>(x => x.Id);

            modelBuilder.Entity<ParameterEntity>().Property(x => x.ParameterType).HasConversion<string>();
            modelBuilder.Entity<ParameterEntity>().HasOne(x => x.Device).WithMany(x => x.Parameters);

            modelBuilder.Entity<DataGroupEntity>().HasOne(x => x.Device).WithMany(x => x.DataGroups);
        }
    }
}
