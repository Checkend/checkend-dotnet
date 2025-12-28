using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;

namespace Checkend.Integrations.AspNetCore;

/// <summary>
/// Extension methods for integrating Checkend with ASP.NET Core.
/// </summary>
public static class CheckendExtensions
{
    /// <summary>
    /// Add Checkend services to the service collection.
    /// </summary>
    public static IServiceCollection AddCheckend(this IServiceCollection services, Action<Configuration.Builder> configure)
    {
        var builder = new Configuration.Builder();
        configure(builder);
        CheckendClient.Configure(builder.Build());
        return services;
    }

    /// <summary>
    /// Add Checkend services using configuration from appsettings.json.
    /// </summary>
    public static IServiceCollection AddCheckend(this IServiceCollection services, IConfiguration configuration)
    {
        var section = configuration.GetSection("Checkend");

        var builder = new Configuration.Builder();

        var apiKey = section["ApiKey"];
        if (!string.IsNullOrEmpty(apiKey))
        {
            builder.ApiKey(apiKey);
        }

        var endpoint = section["Endpoint"];
        if (!string.IsNullOrEmpty(endpoint))
        {
            builder.Endpoint(endpoint);
        }

        var environment = section["Environment"];
        if (!string.IsNullOrEmpty(environment))
        {
            builder.Environment(environment);
        }

        if (bool.TryParse(section["Enabled"], out var enabled))
        {
            builder.Enabled(enabled);
        }

        if (bool.TryParse(section["Debug"], out var debug))
        {
            builder.Debug(debug);
        }

        CheckendClient.Configure(builder.Build());
        return services;
    }

    /// <summary>
    /// Use Checkend middleware for automatic error reporting.
    /// </summary>
    public static IApplicationBuilder UseCheckend(this IApplicationBuilder app)
    {
        return app.UseMiddleware<CheckendMiddleware>();
    }
}
