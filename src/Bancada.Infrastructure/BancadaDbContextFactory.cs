using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Bancada.Infrastructure;

public sealed class BancadaDbContextFactory : IDesignTimeDbContextFactory<BancadaDbContext>
{
    public BancadaDbContext CreateDbContext(string[] args)
    {
        var connectionString = Environment.GetEnvironmentVariable("ConnectionStrings__Bancada");
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException(
                "Set ConnectionStrings__Bancada to a direct PostgreSQL connection before running EF Core tools.");
        }

        var options = new DbContextOptionsBuilder<BancadaDbContext>()
            .UseNpgsql(connectionString)
            .Options;

        return new BancadaDbContext(options);
    }
}
