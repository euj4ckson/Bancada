using System.Data.Common;
using Bancada.Infrastructure;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;

namespace Bancada.Api.Tests;

public sealed class BancadaWebApplicationFactory : WebApplicationFactory<Program>
{
    private readonly DbConnection _connection = new SqliteConnection("Data Source=:memory:");

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        _connection.Open();
        builder.UseEnvironment("Testing");
        builder.ConfigureLogging(logging => logging.ClearProviders());
        builder.ConfigureServices(services =>
        {
            services.RemoveAll<DbContextOptions<BancadaDbContext>>();
            services.RemoveAll<BancadaDbContext>();
            services.AddDbContext<BancadaDbContext>(options => options.UseSqlite(_connection));
        });
    }

    public HttpClient CreateIsolatedClient()
    {
        var client = CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("https://localhost"),
            HandleCookies = true,
            AllowAutoRedirect = false
        });

        using var scope = Services.CreateScope();
        scope.ServiceProvider.GetRequiredService<BancadaDbContext>().Database.EnsureCreated();
        return client;
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (disposing)
        {
            _connection.Dispose();
        }
    }
}
