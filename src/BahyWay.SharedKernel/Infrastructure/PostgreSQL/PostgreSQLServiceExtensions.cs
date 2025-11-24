using BahyWay.SharedKernel.Application.Abstractions;
using Microsoft.Extensions.DependencyInjection;

namespace BahyWay.SharedKernel.Infrastructure.PostgreSQL;

/// <summary>
/// Extension methods for registering PostgreSQL HA services.
/// </summary>
public static class PostgreSQLServiceExtensions
{
    /// <summary>
    /// Registers the PostgreSQL HA health monitoring service in the dependency injection container.
    /// </summary>
    /// <param name="services">The service collection to add services to.</param>
    /// <returns>The service collection for chaining.</returns>
    /// <remarks>
    /// This method registers the IPostgreSQLHealthService with a scoped lifetime,
    /// which is appropriate for services that maintain state during a request.
    /// </remarks>
    public static IServiceCollection AddPostgreSQLHealthMonitoring(this IServiceCollection services)
    {
        services.AddScoped<IPostgreSQLHealthService, PostgreSQLHealthService>();
        return services;
    }
}
