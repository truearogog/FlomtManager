using FlomtManager.Core;
using FlomtManager.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace FlomtManager.Infrastructure;

public class AppDb(DbContextOptions<AppDb> dbContextOptions) : DbContext(dbContextOptions), IAppDb
{
    public DbSet<Device> Devices { get; set; }
    public DbSet<DeviceDefinition> DeviceDefinitions { get; set; }
    public DbSet<Parameter> Parameters { get; set; }
    public DbSet<DataGroup> DataGroups { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Device>().HasMany(x => x.Parameters).WithOne(x => x.Device);
        modelBuilder.Entity<Device>().HasMany(x => x.DataGroups).WithOne(x => x.Device);
        modelBuilder.Entity<Device>().HasOne(x => x.DeviceDefinition).WithOne().HasForeignKey<DeviceDefinition>(x => x.Id);

        modelBuilder.Entity<Parameter>().Property(x => x.ParameterType).HasConversion<string>();
        modelBuilder.Entity<Parameter>().Property(x => x.Name).HasMaxLength(4);
        modelBuilder.Entity<Parameter>().Property(x => x.Unit).HasMaxLength(6);

        modelBuilder.Entity<DataGroup>().HasIndex(x => new { x.DeviceId, x.DateTime }).IsUnique();
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        IEnumerable<EntityEntry> entries = ChangeTracker
            .Entries()
            .Where(entry => entry is { Entity: IEntity, State: EntityState.Added or EntityState.Modified });

        foreach (EntityEntry entry in entries)
        {
            if (entry.State is EntityState.Added)
            {
                ((IEntity)entry.Entity).Created = DateTime.UtcNow;
            }
            ((IEntity)entry.Entity).Updated = DateTime.UtcNow;
        }

        return base.SaveChangesAsync(cancellationToken);
    }
}
