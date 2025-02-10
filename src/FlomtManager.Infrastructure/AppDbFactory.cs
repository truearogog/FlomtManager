using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace FlomtManager.Infrastructure;

internal sealed class AppDbFactory : IDesignTimeDbContextFactory<AppDb>
{
    public AppDb CreateDbContext(string[] args)
    {
        var builder = new DbContextOptionsBuilder<AppDb>();
        var folder = Environment.SpecialFolder.LocalApplicationData;
        var path = Path.Join(Environment.GetFolderPath(folder), "app.db");
        builder.UseSqlite($"Data Source={path}");
        return new AppDb(builder.Options);
    }
}
