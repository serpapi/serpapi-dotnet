using System.Net;
using System.Text.Json;

namespace SerpApi;

/// <summary>
/// Official .NET client for SerpApi. Supports 100+ search engines.
/// </summary>
/// <example>
/// <code>
/// using var client = new SerpApiClient("your_api_key");
/// var result = await client.SearchAsync(new Dictionary&lt;string, string&gt;
/// {
///     ["engine"] = "google",
///     ["q"] = "coffee"
/// });
/// </code>
/// </example>
public sealed class SerpApiClient : IDisposable
{
    private const string DefaultSource = "dotnet";

    private readonly HttpClient _httpClient;
    private readonly SerpApiClientOptions _options;
    private readonly bool _ownsHttpClient;

    /// <summary>
    /// Creates a new SerpApi client with the specified API key.
    /// </summary>
    /// <param name="apiKey">Your SerpApi API key from https://serpapi.com/manage-api-key</param>
    /// <param name="options">Optional configuration overrides.</param>
    public SerpApiClient(string apiKey, SerpApiClientOptions? options = null)
    {
        if (string.IsNullOrWhiteSpace(apiKey))
            throw new SerpApiKeyException("API key must not be empty. Get one at https://serpapi.com/manage-api-key");

        _options = new SerpApiClientOptions
        {
            ApiKey = apiKey,
            BaseUrl = options?.BaseUrl ?? "https://serpapi.com",
            Timeout = options?.Timeout ?? TimeSpan.FromSeconds(60)
        };
        _httpClient = new HttpClient { Timeout = _options.Timeout };
        _ownsHttpClient = true;
    }

    /// <summary>
    /// Creates a new SerpApi client using an externally managed HttpClient (for DI/IHttpClientFactory).
    /// </summary>
    /// <param name="httpClient">Pre-configured HttpClient instance.</param>
    /// <param name="options">Configuration including API key.</param>
    public SerpApiClient(HttpClient httpClient, SerpApiClientOptions options)
    {
        if (options is null)
            throw new ArgumentNullException(nameof(options));

        if (string.IsNullOrWhiteSpace(options.ApiKey))
            throw new SerpApiKeyException("API key must not be empty. Get one at https://serpapi.com/manage-api-key");

        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _options = new SerpApiClientOptions
        {
            ApiKey = options.ApiKey,
            BaseUrl = options.BaseUrl,
            Timeout = options.Timeout
        };
        _ownsHttpClient = false;
    }

    /// <summary>
    /// Execute a search and return parsed JSON results.
    /// </summary>
    /// <param name="parameters">Search parameters. Must include "engine" key.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Parsed response with convenience accessors.</returns>
    public async Task<SerpApiResponse> SearchAsync(
        Dictionary<string, string> parameters,
        CancellationToken cancellationToken = default)
    {
        var url = BuildUrl("/search", parameters, outputJson: true);
        var json = await GetStringAsync(url, cancellationToken).ConfigureAwait(false);
        var response = ParseResponse(json);
        try
        {
            ThrowIfError(response);
            return response;
        }
        catch
        {
            response.Dispose();
            throw;
        }
    }

    /// <summary>
    /// Execute a search and return raw HTML.
    /// </summary>
    public async Task<string> HtmlAsync(
        Dictionary<string, string> parameters,
        CancellationToken cancellationToken = default)
    {
        var url = BuildUrl("/search", parameters, outputJson: false);
        return await GetStringAsync(url, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Retrieve a previous search result from the archive (free, 0 credits).
    /// </summary>
    /// <param name="searchId">The search ID from SearchMetadata.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    public async Task<SerpApiResponse> SearchArchiveAsync(
        string searchId,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(searchId))
            throw new ArgumentException("searchId must not be empty.", nameof(searchId));

        var url = BuildUrl($"/searches/{Uri.EscapeDataString(searchId)}.json", new Dictionary<string, string>(), outputJson: true);
        var json = await GetStringAsync(url, cancellationToken).ConfigureAwait(false);
        var response = ParseResponse(json);
        try
        {
            ThrowIfError(response);
            return response;
        }
        catch
        {
            response.Dispose();
            throw;
        }
    }

    /// <summary>
    /// Get account information (0 credits).
    /// </summary>
    public async Task<SerpApiResponse> AccountAsync(CancellationToken cancellationToken = default)
    {
        var url = BuildUrl("/account", new Dictionary<string, string>(), outputJson: true);
        var json = await GetStringAsync(url, cancellationToken).ConfigureAwait(false);
        var response = ParseResponse(json);
        try
        {
            ThrowIfError(response);
            return response;
        }
        catch
        {
            response.Dispose();
            throw;
        }
    }

    /// <summary>
    /// Get supported locations matching a query.
    /// </summary>
    /// <param name="query">Location search query (e.g., "Austin, TX").</param>
    /// <param name="limit">Max number of results.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    public async Task<JsonElement> LocationAsync(
        string query,
        int limit = 5,
        CancellationToken cancellationToken = default)
    {
        var parameters = new Dictionary<string, string>
        {
            ["q"] = query,
            ["limit"] = limit.ToString()
        };

        var url = BuildUrl("/locations.json", parameters, outputJson: true, includeOutput: false);
        var json = await GetStringAsync(url, cancellationToken).ConfigureAwait(false);

        try
        {
            using var doc = JsonDocument.Parse(json);
            return doc.RootElement.Clone();
        }
        catch (JsonException ex)
        {
            throw new SerpApiException($"Failed to parse location response: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Fetch the next page of results from a previous search response.
    /// </summary>
    /// <param name="response">A previous SerpApiResponse that contains pagination info.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The next page of results, or null if no next page exists.</returns>
    public async Task<SerpApiResponse?> NextPageAsync(
        SerpApiResponse response,
        CancellationToken cancellationToken = default)
    {
        if (response is null)
            throw new ArgumentNullException(nameof(response));

        var nextUrl = response.NextPageUrl;
        if (string.IsNullOrEmpty(nextUrl))
            return null;

        return await FetchPageByUrlAsync(nextUrl!, cancellationToken).ConfigureAwait(false);
    }

    private async Task<SerpApiResponse> FetchPageByUrlAsync(
        string pageUrl,
        CancellationToken cancellationToken)
    {
        // Validate URL origin to prevent SSRF / API key exfiltration
        if (!Uri.TryCreate(pageUrl, UriKind.Absolute, out var parsedUri))
            throw new SerpApiException($"Invalid pagination URL: {pageUrl}");

        var expectedHost = new Uri(_options.BaseUrl).Host;
        if (!string.Equals(parsedUri.Host, expectedHost, StringComparison.OrdinalIgnoreCase))
            throw new SerpApiException($"Pagination URL host '{parsedUri.Host}' does not match expected '{expectedHost}'.");

        if (parsedUri.Scheme != "https" && parsedUri.Scheme != "http")
            throw new SerpApiException($"Pagination URL must use HTTP(S), got '{parsedUri.Scheme}'.");

        var separator = pageUrl.Contains('?') ? "&" : "?";
        var url = $"{pageUrl}{separator}api_key={Uri.EscapeDataString(_options.ApiKey!)}&source={DefaultSource}";
        var json = await GetStringAsync(url, cancellationToken).ConfigureAwait(false);
        var result = ParseResponse(json);
        try
        {
            ThrowIfError(result);
            return result;
        }
        catch
        {
            result.Dispose();
            throw;
        }
    }

    /// <summary>
    /// Enumerate all pages of search results as an async stream.
    /// Caller owns each yielded response and should dispose it.
    /// </summary>
    /// <param name="parameters">Search parameters.</param>
    /// <param name="maxPages">Maximum number of pages to retrieve.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    public IAsyncEnumerable<SerpApiResponse> SearchPagesAsync(
        Dictionary<string, string> parameters,
        int maxPages = 100,
        CancellationToken cancellationToken = default)
    {
        if (maxPages <= 0)
            throw new ArgumentOutOfRangeException(nameof(maxPages), "maxPages must be at least 1.");

        return SearchPagesIteratorAsync(parameters, maxPages, cancellationToken);
    }

    private async IAsyncEnumerable<SerpApiResponse> SearchPagesIteratorAsync(
        Dictionary<string, string> parameters,
        int maxPages,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var current = await SearchAsync(parameters, cancellationToken).ConfigureAwait(false);
        var nextUrl = current.NextPageUrl;
        yield return current;

        for (int i = 1; i < maxPages; i++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (string.IsNullOrEmpty(nextUrl))
                yield break;

            current = await FetchPageByUrlAsync(nextUrl!, cancellationToken).ConfigureAwait(false);
            nextUrl = current.NextPageUrl;
            yield return current;
        }
    }

    // --- Synchronous convenience wrappers ---
    // NOTE: Safe in console apps and ASP.NET Core. May deadlock in legacy
    // frameworks with a SynchronizationContext (WinForms, WPF, ASP.NET 4.x).

    /// <summary>
    /// Execute a search synchronously. Prefer <see cref="SearchAsync"/> for non-blocking usage.
    /// </summary>
    public SerpApiResponse Search(Dictionary<string, string> parameters)
        => Task.Run(() => SearchAsync(parameters)).GetAwaiter().GetResult();

    /// <summary>
    /// Get HTML results synchronously.
    /// </summary>
    public string Html(Dictionary<string, string> parameters)
        => Task.Run(() => HtmlAsync(parameters)).GetAwaiter().GetResult();

    /// <summary>
    /// Get search archive synchronously.
    /// </summary>
    public SerpApiResponse SearchArchive(string searchId)
        => Task.Run(() => SearchArchiveAsync(searchId)).GetAwaiter().GetResult();

    /// <summary>
    /// Get account info synchronously.
    /// </summary>
    public SerpApiResponse Account()
        => Task.Run(() => AccountAsync()).GetAwaiter().GetResult();

    /// <summary>
    /// Get locations synchronously.
    /// </summary>
    public JsonElement Location(string query, int limit = 5)
        => Task.Run(() => LocationAsync(query, limit)).GetAwaiter().GetResult();

    // --- Private helpers ---

    private string BuildUrl(
        string endpoint,
        Dictionary<string, string> parameters,
        bool outputJson,
        bool includeOutput = true)
    {
        var queryParts = new List<string>();

        // Add API key
        queryParts.Add($"api_key={Uri.EscapeDataString(_options.ApiKey!)}");

        // Add user parameters
        foreach (var kvp in parameters)
        {
            if (string.Equals(kvp.Key, "api_key", StringComparison.OrdinalIgnoreCase))
                continue; // already added
            queryParts.Add($"{Uri.EscapeDataString(kvp.Key)}={Uri.EscapeDataString(kvp.Value)}");
        }

        // Add output format
        if (includeOutput)
            queryParts.Add($"output={( outputJson ? "json" : "html" )}");

        // Add source identifier
        queryParts.Add($"source={DefaultSource}");

        return $"{_options.BaseUrl.TrimEnd('/')}{endpoint}?{string.Join("&", queryParts)}";
    }

    private async Task<string> GetStringAsync(string url, CancellationToken cancellationToken)
    {
        try
        {
            using var response = await _httpClient.GetAsync(url, cancellationToken).ConfigureAwait(false);

            var content = await response.Content.ReadAsStringAsync(
#if NET7_0_OR_GREATER
                cancellationToken
#endif
            // netstandard2.0: ReadAsStringAsync has no CancellationToken overload
            ).ConfigureAwait(false);

            if (!response.IsSuccessStatusCode)
            {
                string errorMessage = content;
                try
                {
                    using var doc = JsonDocument.Parse(content);
                    if (doc.RootElement.TryGetProperty("error", out var errorProp))
                        errorMessage = errorProp.GetString() ?? content;
                }
                catch { /* not JSON, use raw content */ }

                if (errorMessage.IndexOf("Invalid API key", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    throw new SerpApiKeyException(errorMessage);
                }

                throw new SerpApiHttpException((int)response.StatusCode, errorMessage);
            }

            return content;
        }
        catch (TaskCanceledException ex) when (!cancellationToken.IsCancellationRequested)
        {
            throw new SerpApiTimeoutException(
                $"Request timed out after {_httpClient.Timeout.TotalSeconds}s", ex);
        }
        catch (HttpRequestException ex)
        {
            throw new SerpApiException($"HTTP request failed: {ex.Message}", ex);
        }
    }

    private static void ThrowIfError(SerpApiResponse response)
    {
        var error = response["error"];
        if (error != null && error.Value.ValueKind == JsonValueKind.String)
        {
            var message = error.Value.GetString()!;
            if (message.IndexOf("Invalid API key", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                throw new SerpApiKeyException(message);
            }
            throw new SerpApiException(message);
        }
    }

    private static SerpApiResponse ParseResponse(string json)
    {
        try
        {
            return new SerpApiResponse(json);
        }
        catch (JsonException ex)
        {
            throw new SerpApiException($"Failed to parse response: {ex.Message}", ex);
        }
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (_ownsHttpClient)
            _httpClient.Dispose();
    }
}
