using Microsoft.Extensions.DependencyInjection;
using AlarmInsight.Infrastructure.Services;

namespace AlarmInsight.Infrastructure
{
    /// <summary>
    /// Dependency Injection configuration for PostgreSQL HA services
    /// Add this to your AlarmInsight.Infrastructure project
    /// </summary>
    public static class PostgreSQLHealthServiceExtensions
    {
        /// <summary>
        /// Registers PostgreSQL Health Service in DI container
        /// </summary>
        /// <param name="services">The service collection</param>
        /// <returns>The service collection for chaining</returns>
        public static IServiceCollection AddPostgreSQLHealthServices(
            this IServiceCollection services)
        {
            // Register the PostgreSQL Health Service
            // Using Scoped lifetime to manage PowerShell runspace lifecycle
            services.AddScoped<IPostgreSQLHealthService, PostgreSQLHealthService>();

            return services;
        }
    }
}

/*
 * USAGE IN Program.cs (or Startup.cs for .NET 5 and earlier):
 *
 * using AlarmInsight.Infrastructure;
 *
 * var builder = WebApplication.CreateBuilder(args);
 *
 * // Add services to the container
 * builder.Services.AddControllers();
 * builder.Services.AddPostgreSQLHealthServices(); // <-- Add this line
 *
 * var app = builder.Build();
 *
 * // Configure the HTTP request pipeline
 * app.MapControllers();
 * app.Run();
 */

/*
 * ALTERNATIVE: Register directly in Program.cs without extension method:
 *
 * builder.Services.AddScoped<IPostgreSQLHealthService, PostgreSQLHealthService>();
 */
