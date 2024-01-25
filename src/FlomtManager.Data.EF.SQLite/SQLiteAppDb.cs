using FlomtManager.Data.EF.Entities;
using Microsoft.EntityFrameworkCore;

namespace FlomtManager.Data.EF.SQLite;

public sealed class SQLiteAppDb(DbContextOptions<SQLiteAppDb> options) : AppDb<SQLiteAppDb>(options)
{
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<DeviceEntity>().Property(x => x.Created).HasDefaultValue(DateTime.UtcNow);
        modelBuilder.Entity<DeviceEntity>().Property(x => x.Updated).HasDefaultValue(DateTime.UtcNow);
    }
}
