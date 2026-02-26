using Microsoft.Extensions.DependencyInjection;
using System;

namespace Cannery.Conductor.Client;

public static class ConfigureServices
{
    /// <summary>
    /// Registers a named HttpClient for MCP with base address. Consumers can add handlers externally.
    /// Example usage:
    ///   services.AddMcpHttpClient("Curator", new Uri("https://localhost:5007"));
    ///   services.AddHttpClient("Curator").AddHttpMessageHandler<TokenHandler>(); // from your other package
    /// </summary>
    public static IServiceCollection AddMcpHttpClient(this IServiceCollection services, string name, Uri baseAddress, TimeSpan? timeout = null)
    {
        services.AddHttpClient(name, http =>
        {
            http.BaseAddress = baseAddress;
            if (timeout.HasValue) http.Timeout = timeout.Value;
        });
        return services;
    }
}
