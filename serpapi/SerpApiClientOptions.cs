namespace SerpApi;

/// <summary>
/// Configuration options for <see cref="SerpApiClient"/>.
/// </summary>
public sealed class SerpApiClientOptions
{
    /// <summary>
    /// SerpApi API key. Obtain from https://serpapi.com/manage-api-key
    /// </summary>
    public string? ApiKey { get; set; }

    /// <summary>
    /// Base URL for the SerpApi service. Defaults to https://serpapi.com.
    /// </summary>
    public string BaseUrl { get; set; } = "https://serpapi.com";

    /// <summary>
    /// HTTP request timeout. Defaults to 60 seconds.
    /// </summary>
    public TimeSpan Timeout { get; set; } = TimeSpan.FromSeconds(60);
}
