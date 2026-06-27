# SerpApi .NET SDK

[![NuGet](https://img.shields.io/nuget/v/serpapi)](https://www.nuget.org/packages/serpapi)
[![Build](https://github.com/serpapi/serpapi-dotnet/actions/workflows/ci.yml/badge.svg)](https://github.com/serpapi/serpapi-dotnet/actions/workflows/ci.yml)
[![License: MIT](https://img.shields.io/badge/License-MIT-green.svg)](https://github.com/serpapi/serpapi-dotnet/blob/master/LICENSE)

Integrate search data into your .NET application, AI workflow, or RAG pipeline. This is the official .NET client for [SerpApi](https://serpapi.com).

SerpApi supports Google, Google Maps, Google Shopping, Bing, Baidu, Yandex, Yahoo, DuckDuckGo, eBay, Walmart, YouTube, and [100+ engines](https://serpapi.com).

## Installation

```bash
dotnet add package serpapi
```

Targets **.NET 7.0** and **.NET 9.0**.

## Quick Start

```csharp
using SerpApi;

using var client = new SerpApiClient("YOUR_API_KEY");

var results = await client.SearchAsync(new Dictionary<string, string>
{
    ["engine"] = "google",
    ["q"] = "coffee",
    ["location"] = "Austin, Texas"
});

foreach (var result in results.OrganicResults!.Value.EnumerateArray())
{
    Console.WriteLine(result.GetProperty("title").GetString());
}
```

Get your API key at [serpapi.com/manage-api-key](https://serpapi.com/manage-api-key).

## Usage

### Search (async)

```csharp
using var client = new SerpApiClient(Environment.GetEnvironmentVariable("SERPAPI_KEY")!);

var results = await client.SearchAsync(new Dictionary<string, string>
{
    ["engine"] = "google",
    ["q"] = "coffee"
});

Console.WriteLine(results.SearchId);          // search ID for archive
Console.WriteLine(results.OrganicResults);    // organic results array
Console.WriteLine(results["local_results"]);  // any field by key
```

### Search (sync)

```csharp
var results = client.Search(new Dictionary<string, string>
{
    ["engine"] = "google",
    ["q"] = "coffee"
});
```

### HTML output

```csharp
string html = await client.HtmlAsync(new Dictionary<string, string>
{
    ["engine"] = "google",
    ["q"] = "coffee"
});
```

### Search Archive (0 credits)

```csharp
var archived = await client.SearchArchiveAsync("previous_search_id");
```

### Account Info (0 credits)

```csharp
var account = await client.AccountAsync();
Console.WriteLine(account["plan_id"]);
```

### Locations

```csharp
var locations = await client.LocationAsync("Austin, TX", limit: 3);
```

### Pagination

Fetch the next page of results:

```csharp
var page1 = await client.SearchAsync(new Dictionary<string, string>
{
    ["engine"] = "google",
    ["q"] = "coffee"
});

var page2 = await client.NextPageAsync(page1);
```

Or iterate all pages as an async stream (.NET 8+):

```csharp
await foreach (var page in client.SearchPagesAsync(
    new Dictionary<string, string> { ["engine"] = "google", ["q"] = "coffee" },
    maxPages: 5))
{
    Console.WriteLine($"Page has {page.OrganicResults?.Value.GetArrayLength()} results");
}
```

## Supported Engines

Pass the engine name as the `"engine"` parameter:

| Engine | Value |
|--------|-------|
| Google | `google` |
| Google Maps | `google_maps` |
| Google Images | `google_images` |
| Google Scholar | `google_scholar` |
| Google Jobs | `google_jobs` |
| Google Shopping | `google_shopping` |
| Google News | `google_news` |
| Google Autocomplete | `google_autocomplete` |
| Google Events | `google_events` |
| Google Play | `google_play` |
| Google Local Services | `google_local_services` |
| Google Reverse Image | `google_reverse_image` |
| Bing | `bing` |
| Baidu | `baidu` |
| Yahoo | `yahoo` |
| Yandex | `yandex` |
| DuckDuckGo | `duckduckgo` |
| eBay | `ebay` |
| Walmart | `walmart` |
| YouTube | `youtube` |
| Amazon | `amazon` |
| Apple App Store | `apple_app_store` |
| Home Depot | `home_depot` |
| Naver | `naver` |

See [serpapi.com](https://serpapi.com) for the full list.

## Error Handling

```csharp
try
{
    var results = await client.SearchAsync(params);
}
catch (SerpApiKeyException ex)
{
    // Invalid or missing API key
    Console.WriteLine($"Auth error: {ex.Message}");
}
catch (SerpApiHttpException ex)
{
    // HTTP error (429 rate limit, 500 server error, etc.)
    Console.WriteLine($"HTTP {ex.StatusCode}: {ex.Message}");
}
catch (SerpApiTimeoutException ex)
{
    // Request timed out
    Console.WriteLine($"Timeout: {ex.Message}");
}
catch (SerpApiException ex)
{
    // Any other SerpApi error
    Console.WriteLine($"Error: {ex.Message}");
}
```

## Configuration

```csharp
var options = new SerpApiClientOptions
{
    Timeout = TimeSpan.FromSeconds(30)
};

using var client = new SerpApiClient("YOUR_API_KEY", options);
```

## Dependency Injection

Register with `IServiceCollection` for ASP.NET Core / generic host:

```csharp
builder.Services.AddSerpApi(options =>
{
    options.ApiKey = builder.Configuration["SerpApi:ApiKey"]!;
    options.Timeout = TimeSpan.FromSeconds(30);
});
```

Then inject `SerpApiClient` anywhere:

```csharp
public class SearchService
{
    private readonly SerpApiClient _client;

    public SearchService(SerpApiClient client) => _client = client;

    public async Task<SerpApiResponse> SearchAsync(string query)
    {
        return await _client.SearchAsync(new Dictionary<string, string>
        {
            ["engine"] = "google",
            ["q"] = query
        });
    }
}
```

This uses `IHttpClientFactory` under the hood for proper connection management.

## Response

`SerpApiResponse` wraps the JSON response with convenience accessors. It implements `IDisposable` to release pooled JSON memory:

```csharp
using var results = await client.SearchAsync(params);

results.SearchId            // string? — search ID
results.SearchMetadata      // JsonElement? — metadata block
results.OrganicResults      // JsonElement? — organic results array
results.Pagination          // JsonElement? — pagination info
results.NextPageUrl         // string? — URL for next page
results["any_key"]          // JsonElement? — any top-level field
results.RawJson             // string — raw JSON
results.GetProperty<T>(key) // T? — deserialize a property
results.As<T>()             // T? — deserialize entire response
```

## Development

```bash
dotnet restore
dotnet build
dotnet test
dotnet pack
```

## License

MIT — see [LICENSE](LICENSE).
