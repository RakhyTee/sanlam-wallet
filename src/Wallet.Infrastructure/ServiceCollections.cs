using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Wallet.Application;
using Wallet.Application.Abstractions;
using Wallet.Infrastructure.Events;
using Wallet.Infrastructure.Persistence;
using Wallet.Infrastructure.Repositories;

namespace Wallet.Infrastructure;

public static class ServiceCollections
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("Wallet")
            ?? throw new InvalidOperationException("Connection string 'Wallet' was not found.");

        services.AddDbContext<WalletDbContext>(options => options.UseSqlite(connectionString));

        services.AddScoped<IWalletRepository, WalletRepository>();
        services.AddScoped<WalletSeeder>();
        services.AddScoped<IEventPublisher, DbEventPublisher>();

        services.AddSingleton(TimeProvider.System);

        return services;
    }
}
