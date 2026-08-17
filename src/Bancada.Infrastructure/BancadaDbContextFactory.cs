using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Bancada.Infrastructure;

public sealed class BancadaDbContextFactory : IDesignTimeDbContextFactory<BancadaDbContext>
{
    public BancadaDbContext CreateDbContext(string[] args)
    {
        var connectionString = Environment.GetEnvironmentVariable("ConnectionStrings__Bancada")
            ?? "Host=localhost;Port=5432;Database=bancada;Username=postgres";
        var options = new DbContextOptionsBuilder<BancadaDbContext>()
            .UseNpgsql(connectionString)
            .Options;

        return new BancadaDbContext(options);
    }
}
