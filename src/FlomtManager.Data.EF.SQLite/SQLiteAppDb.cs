using FlomtManager.Data.EF.Entities;
using FlomtManager.Data.EF.Entities.Base;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FlomtManager.Data.EF.SQLite;

public sealed class SQLiteAppDb(DbContextOptions<SQLiteAppDb> options) : AppDb<SQLiteAppDb>(options)
{
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        BasePropertyConfiguration(modelBuilder.Entity<DeviceEntity>());
        BasePropertyConfiguration(modelBuilder.Entity<DeviceDefinitionEntity>());
        BasePropertyConfiguration(modelBuilder.Entity<ParameterEntity>());
    }

    private static void BasePropertyConfiguration<T>(EntityTypeBuilder<T> entityTypeBuilder) where T : EntityBase
    {
        entityTypeBuilder.HasKey(x => x.Id);
        entityTypeBuilder.Property(x => x.Created).HasColumnType("datetime").HasDefaultValueSql("CURRENT_TIMESTAMP");
        entityTypeBuilder.Property(x => x.Updated).HasColumnType("datetime").HasDefaultValueSql("CURRENT_TIMESTAMP");
    }
}
