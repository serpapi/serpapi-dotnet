# SerpApi .NET Library

[![NuGet](https://img.shields.io/nuget/v/serpapi)](https://www.nuget.org/packages/serpapi)
[![Build](https://github.com/serpapi/serpapi-dotnet/actions/workflows/ci.yml/badge.svg)](https://github.com/serpapi/serpapi-dotnet/actions/workflows/ci.yml)

> **Not yet published.** The `serpapi` package hasn't shipped its first NuGet release — the badge above will go green once v1.0.0 is out. Until then, build from source (see [Contributing](#contributing)) or reference the project directly.

Integrate search data into your .NET application, AI workflow, or LLM/RAG pipeline. This is the official .NET client for [SerpApi](https://serpapi.com).

SerpApi supports Google, Google Maps, Google Shopping, Bing, Baidu, Yandex, Yahoo, DuckDuckGo, eBay, Walmart, YouTube, and [100+ engines](https://serpapi.com).

## Features

- Async-first with full `CancellationToken` support
- Sync convenience wrappers
- `IAsyncEnumerable` pagination
- Dependency injection integration (`IHttpClientFactory`)
- Targets .NET Standard 2.0, .NET 7, 8, 9, and 10
- Zero external runtime dependencies

## Compatibility

| Target framework | Minimum consumer runtime | Notes |
|---|---|---|
| `netstandard2.0` | .NET Framework 4.6.1+, .NET Core 2.0+, Mono, Xamarin, UWP | Ships `Microsoft.Bcl.AsyncInterfaces` and `System.Text.Json` as polyfills |
| `net7.0` | .NET 7 | Out of support upstream, still built and tested |
| `net8.0` | .NET 8 (LTS) | |
| `net9.0` | .NET 9 (STS) | |
| `net10.0` | .NET 10 (LTS) | Used for `dotnet pack` and local dev builds |

CI builds and tests every target framework above on both **Linux** and **Windows**. Building locally requires the [.NET 10 SDK](https://dotnet.microsoft.com/download) (a single SDK at or above the highest TFM can build all lower ones).

## Installation

```bash
dotnet add package serpapi
```

## Migrating from google-search-results-dotnet

`serpapi` replaces [`google-search-results-dotnet`](https://github.com/serpapi/google-search-results-dotnet), the previous official client. New package ID — install `serpapi` instead; there's no automatic upgrade path.

| | Old ([`google-search-results-dotnet`](https://github.com/serpapi/google-search-results-dotnet)) | New ([`serpapi-dotnet`](https://github.com/serpapi/serpapi-dotnet)) |
|---|---|---|
| Client | One class per engine: `GoogleSearch`, `BingSearch`, `BaiduSearch`, `YahooSearch`, `YandexSearch`, `EbaySearch`, or generic `SerpApiSearch(parameter, apiKey, engine)` | One `SerpApiClient` for every engine — set `["engine"] = "google"` in the parameters |
| Construction | `new GoogleSearch(Hashtable parameter, string apiKey)` | `new SerpApiClient(string apiKey)` |
| Parameters | `Hashtable` | `Dictionary<string, string>` |
| Search | `JObject data = search.GetJson();` — synchronous | `await client.SearchAsync(parameters)` → `SerpApiResponse` (sync `Search(...)` also available) |
| Archive | `search.GetSearchArchiveJson(id)` | `await client.SearchArchiveAsync(id)` |
| Account | `search.GetAccount()` | `await client.AccountAsync()` |
| Locations | `search.GetLocation(query, limit)` | `await client.LocationAsync(query, limit)` |
| Timeout | `search.setTimeoutSeconds(int)` | `SerpApiClientOptions.Timeout` at construction |
| Cleanup | `search.Close()` | `using var client = ...` (implements `IDisposable`) |
| Result type | `Newtonsoft.Json.Linq.JObject`/`JArray` | `System.Text.Json`-based `SerpApiResponse` (indexer, `As<T>()`, `GetProperty<T>()`) |
| Errors | `SerpApiSearchException` | `SerpApiKeyException`, `SerpApiHttpException`, `SerpApiTimeoutException`, `SerpApiException` |
| Pagination | Manual | `NextPageAsync(response)` / `SearchPagesAsync(parameters)` (`IAsyncEnumerable`) |

Before:

```csharp
var ht = new Hashtable { { "q", "coffee" } };
GoogleSearch search = new GoogleSearch(ht, apiKey);
JObject data = search.GetJson();
```

After:

```csharp
using var client = new SerpApiClient(apiKey);
using var results = await client.SearchAsync(new Dictionary<string, string>
{
    ["engine"] = "google",
    ["q"] = "coffee"
});
```

## Simple Usage

```csharp
using SerpApi;

using var client = new SerpApiClient(Environment.GetEnvironmentVariable("SERPAPI_KEY")!);

using var results = await client.SearchAsync(new Dictionary<string, string>
{
    ["engine"] = "google_light",
    ["q"] = "coffee"
});

foreach (var result in results.OrganicResults!.Value.EnumerateArray())
{
    Console.WriteLine(result.GetProperty("title").GetString());
}
```

### Error handling

```csharp
try
{
    using var results = await client.SearchAsync(parameters);
}
catch (SerpApiKeyException)       { /* 401 — invalid API key */ }
catch (SerpApiHttpException ex)   { /* 429, 500, etc — ex.StatusCode */ }
catch (SerpApiTimeoutException)   { /* request timed out */ }
catch (SerpApiException ex)       { /* catch-all */ }
```

## Search API usage

### Get JSON results

```csharp
using var results = await client.SearchAsync(new Dictionary<string, string>
{
    ["engine"] = "google_light",
    ["q"] = "coffee",
    ["num"] = "10"
});

Console.WriteLine(results.SearchId);
Console.WriteLine(results.OrganicResults);
Console.WriteLine(results["local_results"]);
```

### Get HTML results

```csharp
string html = await client.HtmlAsync(new Dictionary<string, string>
{
    ["engine"] = "google_light",
    ["q"] = "coffee"
});
```

### Pagination

```csharp
// Next page
using var page2 = await client.NextPageAsync(results);

// Iterate all pages as an async stream
await foreach (var page in client.SearchPagesAsync(parameters, maxPages: 5))
{
    using (page)
    {
        Console.WriteLine($"Page has {page.OrganicResults?.GetArrayLength()} results");
    }
}
```

### Search concurrently

```csharp
var tasks = new[]
{
    client.SearchAsync(new Dictionary<string, string>
    {
        ["engine"] = "google_light", ["q"] = "coffee"
    }),
    client.SearchAsync(new Dictionary<string, string>
    {
        ["engine"] = "google_news_light", ["q"] = "coffee"
    })
};

try
{
    var results = await Task.WhenAll(tasks);
    // Process results here.
}
finally
{
    foreach (var task in tasks)
    {
        if (task.Status == TaskStatus.RanToCompletion)
            task.Result.Dispose();
    }
}
```

### Location API

```csharp
var locations = await client.LocationAsync("Austin, TX", limit: 3);
foreach (var loc in locations.EnumerateArray())
{
    Console.WriteLine(loc.GetProperty("name").GetString());
}
```

### Search Archive API

Retrieve a previous search (0 credits):

```csharp
using var archived = await client.SearchArchiveAsync("previous_search_id");
```

### Account API

```csharp
using var account = await client.AccountAsync();
Console.WriteLine(account["plan_id"]);
```

## Basic examples per search engine

### Search Google

```csharp
using var results = await client.SearchAsync(new Dictionary<string, string>
{
    ["engine"] = "google",
    ["q"] = "coffee",
    ["location"] = "Austin, Texas"
});
```

* see: https://serpapi.com/search-api

### Search Google Light

```csharp
using var results = await client.SearchAsync(new Dictionary<string, string>
{
    ["engine"] = "google_light",
    ["q"] = "coffee"
});
```

* see: https://serpapi.com/google-light-api

### Search Google Scholar

```csharp
using var results = await client.SearchAsync(new Dictionary<string, string>
{
    ["engine"] = "google_scholar",
    ["q"] = "machine learning"
});
```

* see: https://serpapi.com/google-scholar-api

### Search Google News

```csharp
using var results = await client.SearchAsync(new Dictionary<string, string>
{
    ["engine"] = "google_news",
    ["q"] = "artificial intelligence"
});
```

* see: https://serpapi.com/google-news-api

### Search Google Maps

```csharp
using var results = await client.SearchAsync(new Dictionary<string, string>
{
    ["engine"] = "google_maps",
    ["q"] = "pizza",
    ["ll"] = "@40.7455096,-74.0083012,14z"
});
```

* see: https://serpapi.com/google-maps-api

### Search Google Shopping

```csharp
using var results = await client.SearchAsync(new Dictionary<string, string>
{
    ["engine"] = "google_shopping",
    ["q"] = "laptop"
});
```

* see: https://serpapi.com/google-shopping-api

### Search Google Jobs

```csharp
using var results = await client.SearchAsync(new Dictionary<string, string>
{
    ["engine"] = "google_jobs",
    ["q"] = "software engineer"
});
```

* see: https://serpapi.com/google-jobs-api

### Search Google Images

```csharp
using var results = await client.SearchAsync(new Dictionary<string, string>
{
    ["engine"] = "google_images",
    ["q"] = "sunset"
});
```

* see: https://serpapi.com/images-results

### Search Google Finance

```csharp
using var results = await client.SearchAsync(new Dictionary<string, string>
{
    ["engine"] = "google_finance",
    ["q"] = "AAPL:NASDAQ"
});
```

* see: https://serpapi.com/google-finance-api

### Search Bing

```csharp
using var results = await client.SearchAsync(new Dictionary<string, string>
{
    ["engine"] = "bing",
    ["q"] = "coffee"
});
```

* see: https://serpapi.com/bing-search-api

### Search DuckDuckGo

```csharp
using var results = await client.SearchAsync(new Dictionary<string, string>
{
    ["engine"] = "duckduckgo",
    ["q"] = "coffee"
});
```

* see: https://serpapi.com/duckduckgo-search-api

### Search Baidu

```csharp
using var results = await client.SearchAsync(new Dictionary<string, string>
{
    ["engine"] = "baidu",
    ["q"] = "coffee"
});
```

* see: https://serpapi.com/baidu-search-api

### Search Yahoo

```csharp
using var results = await client.SearchAsync(new Dictionary<string, string>
{
    ["engine"] = "yahoo",
    ["p"] = "coffee"
});
```

* see: https://serpapi.com/yahoo-search-api

### Search YouTube

```csharp
using var results = await client.SearchAsync(new Dictionary<string, string>
{
    ["engine"] = "youtube",
    ["search_query"] = "latte art"
});
```

* see: https://serpapi.com/youtube-search-api

### Search Walmart

```csharp
using var results = await client.SearchAsync(new Dictionary<string, string>
{
    ["engine"] = "walmart",
    ["query"] = "coffee maker"
});
```

* see: https://serpapi.com/walmart-search-api

### Search eBay

```csharp
using var results = await client.SearchAsync(new Dictionary<string, string>
{
    ["engine"] = "ebay",
    ["_nkw"] = "laptop"
});
```

* see: https://serpapi.com/ebay-search-api

### Search Amazon

```csharp
using var results = await client.SearchAsync(new Dictionary<string, string>
{
    ["engine"] = "amazon",
    ["k"] = "coffee"
});
```

* see: https://serpapi.com/amazon-search-api

### Search Naver

```csharp
using var results = await client.SearchAsync(new Dictionary<string, string>
{
    ["engine"] = "naver",
    ["query"] = "coffee"
});
```

* see: https://serpapi.com/naver-search-api

### Search Apple App Store

```csharp
using var results = await client.SearchAsync(new Dictionary<string, string>
{
    ["engine"] = "apple_app_store",
    ["term"] = "coffee"
});
```

* see: https://serpapi.com/apple-app-store

### Search Home Depot

```csharp
using var results = await client.SearchAsync(new Dictionary<string, string>
{
    ["engine"] = "home_depot",
    ["q"] = "drill"
});
```

* see: https://serpapi.com/home-depot-search-api

## Configuration

```csharp
using var client = new SerpApiClient("YOUR_API_KEY", new SerpApiClientOptions
{
    Timeout = TimeSpan.FromSeconds(30)
});
```

### Dependency Injection

```csharp
builder.Services.AddSerpApi(options =>
{
    options.ApiKey = builder.Configuration["SerpApi:ApiKey"]!;
    options.Timeout = TimeSpan.FromSeconds(30);
});
```

Uses `IHttpClientFactory` for connection management.

#### Resilience

Install the `Microsoft.Extensions.Http.Resilience` package:

```bash
dotnet add package Microsoft.Extensions.Http.Resilience
```

```csharp
builder.Services.AddSerpApi(options =>
{
    options.ApiKey = builder.Configuration["SerpApi:ApiKey"]!;
})
.AddStandardResilienceHandler();
```

#### Corporate proxy

```csharp
var handler = new HttpClientHandler
{
    Proxy = new WebProxy("http://proxy.corp.example:8080"),
    UseProxy = true
};
using var client = new SerpApiClient(
    new HttpClient(handler),
    new SerpApiClientOptions { ApiKey = "YOUR_API_KEY" });
```

## Examples

See [`examples/`](examples/) for runnable projects:

| Example | Use Case | Description |
|---------|----------|-------------|
| [LeadFinder](examples/LeadFinder/) | Lead generation | Find local businesses via Google Maps for sales outreach |
| [CompetitorTracker](examples/CompetitorTracker/) | SEO & competitive intel | Monitor brand vs competitor SERP positions across engines |
| [RankTracker](examples/RankTracker/) | SEO rank monitoring | Track keyword positions page-by-page with pagination |
| [PriceMonitor](examples/PriceMonitor/) | Price monitoring | Compare product prices across Google Shopping and Walmart |
| [AiResearchAgent](examples/AiResearchAgent/) | AI/RAG pipelines | Gather multi-source context (web + news + scholar) for LLMs |
| [ContentDiscovery](examples/ContentDiscovery/) | Market research | Find trending topics, PAA questions, and content gaps |
| [ErrorHandling](examples/ErrorHandling/) | Reliability | Exception types, retry patterns, and graceful degradation |
| [DependencyInjection](examples/DependencyInjection/) | Enterprise integration | ASP.NET Core / generic host with `IHttpClientFactory` |

```bash
export SERPAPI_KEY=your_key_here
cd examples/LeadFinder
dotnet run
```

## Contributing

Bug reports and pull requests are welcome on GitHub at https://github.com/serpapi/serpapi-dotnet. See [Compatibility](#compatibility) for SDK requirements.

```bash
git clone https://github.com/serpapi/serpapi-dotnet.git
cd serpapi-dotnet
dotnet build
dotnet test
```

## License

MIT — see [LICENSE](LICENSE).
