using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Elyfe.Smpp.Client;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddSmscClient(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<SmscOptions>(configuration.GetSection("Smsc"));
        services.AddSingleton<ISmscClient, SmscClient>();
        return services;
    }
}
