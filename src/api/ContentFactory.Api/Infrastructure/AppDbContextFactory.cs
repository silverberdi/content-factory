using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace ContentFactory.Api.Infrastructure;

public class AppDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    public AppDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<AppDbContext>();
        var serverVersion = new MySqlServerVersion(new Version(8, 0, 36));
        optionsBuilder.UseMySql(
            "Server=127.0.0.1;Port=3307;Database=content_factory_dev;User=content_factory_dev;Password=;", 
            serverVersion
        );
        return new AppDbContext(optionsBuilder.Options);
    }
}
