using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using SsasMcpServer.Models.Configuration;
using SsasMcpServer.Services.Interfaces;
using SsasMcpServer.Services.Implementations;

namespace SsasMcpServer.Services;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddSsasServices(
        this IServiceCollection services, 
        IConfiguration configuration)
    {
        // Register configuration options
        services.Configure<SsasConnectionOptions>(
            configuration.GetSection(SsasConnectionOptions.SectionName));
        services.Configure<McpServerOptions>(
            configuration.GetSection(McpServerOptions.SectionName));

        // Register memory cache
        services.AddMemoryCache();

        // Register services
        services.AddSingleton<ISsasConnectionService, SsasConnectionService>();
        services.AddSingleton<ICultureService, CultureService>();
        services.AddSingleton<IMetadataService, MetadataService>();
        services.AddSingleton<IDaxQueryBuilder, DaxQueryBuilder>();
        services.AddSingleton<IQueryService, QueryService>();

        return services;
    }
}