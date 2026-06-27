using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace SerpApi;

/// <summary>
/// Extension methods for registering SerpApiClient with Microsoft.Extensions.DependencyInjection.
/// </summary>
public static class SerpApiServiceCollectionExtensions
{
    /// <summary>
    /// Adds SerpApiClient to the service collection with IHttpClientFactory integration.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configure">Action to configure SerpApiClientOptions.</param>
    /// <returns>The IHttpClientBuilder for further configuration (e.g., Polly policies).</returns>
    /// <example>
    /// <code>
    /// services.AddSerpApi(options =>
    /// {
    ///     options.ApiKey = "your_api_key";
    ///     options.Timeout = TimeSpan.FromSeconds(30);
    /// });
    /// </code>
    /// </example>
    public static IHttpClientBuilder AddSerpApi(
        this IServiceCollection services,
        Action<SerpApiClientOptions> configure)
    {
        services.Configure(configure);

        return services.AddHttpClient<SerpApiClient>((sp, httpClient) =>
        {
            var options = sp.GetRequiredService<IOptions<SerpApiClientOptions>>().Value;
            httpClient.Timeout = options.Timeout;
            httpClient.BaseAddress = new Uri(options.BaseUrl);
        })
        .AddTypedClient((httpClient, sp) =>
        {
            var options = sp.GetRequiredService<IOptions<SerpApiClientOptions>>().Value;
            return new SerpApiClient(httpClient, options);
        });
    }
}
