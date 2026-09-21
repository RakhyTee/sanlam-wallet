using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Wallet.Infrastructure.Persistence;

namespace Wallet.Api.Tests;

public sealed class WalletApiFactory : WebApplicationFactory<Program>
{
    public string DbPath { get; } = Path.Combine(Path.GetTempPath(), $"wallet-tests-{Guid.NewGuid():N}.db");

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            var descriptor = services.SingleOrDefault(d => d.ServiceType == typeof(DbContextOptions<WalletDbContext>));
            if (descriptor is not null)
                services.Remove(descriptor);

            services.AddDbContext<WalletDbContext>(options => options.UseSqlite($"Data Source={DbPath}"));
        });
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);

        if (disposing)
        {
            SqliteConnection.ClearAllPools();

            if (File.Exists(DbPath))
                File.Delete(DbPath);
        }
    }
}
