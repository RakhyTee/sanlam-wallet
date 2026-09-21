using FluentValidation;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Wallet.Application.Services;

namespace Wallet.Application;

public static class ServiceCollections
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(ServiceCollections).Assembly));
        services.AddValidatorsFromAssembly(typeof(ServiceCollections).Assembly);
        services.AddScoped<IWalletService, WalletService>();

        return services;
    }
}
