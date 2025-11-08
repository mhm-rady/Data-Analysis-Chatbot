
using Microsoft.Extensions.DependencyInjection;

namespace SsasMcpServer.Tools;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddMcpServerTools(this IServiceCollection services)
    {
        services.AddMcpServer()
            .WithStdioServerTransport()
            .WithToolsFromAssembly(typeof(ServiceCollectionExtensions).Assembly);

        return services;
    }
}