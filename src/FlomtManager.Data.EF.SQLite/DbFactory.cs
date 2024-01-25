#if DEBUG

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace FlomtManager.Data.EF.SQLite;

internal class DbFactory : IDesignTimeDbContextFactory<SQLiteAppDb>
{
    public SQLiteAppDb CreateDbContext(string[] args)
    {
        var builder = new DbContextOptionsBuilder<SQLiteAppDb>();
        var folder = Environment.SpecialFolder.LocalApplicationData;
        var path = Path.Join(Environment.GetFolderPath(folder), "app.db");
        builder.UseSqlite($"Data Source={path}");
        return new SQLiteAppDb(builder.Options);
    }
}
#endif