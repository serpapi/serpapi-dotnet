# SerpApi .NET Library

[![NuGet](https://img.shields.io/nuget/v/serpapi)](https://www.nuget.org/packages/serpapi)
[![Build](https://github.com/serpapi/serpapi-dotnet/actions/workflows/ci.yml/badge.svg)](https://github.com/serpapi/serpapi-dotnet/actions/workflows/ci.yml)

> **Not yet published.** The `serpapi` package hasn't shipped its first NuGet release — the badge above will go green once v1.0.0 is out. Until then, build from source (see [Contributing](#contributing)) or reference the project directly.

Integrate search data into your AI workflow, RAG / fine-tuning, or .NET application using this official wrapper for [SerpApi](https://serpapi.com).

SerpApi supports Google, Google Maps, Google Shopping, Bing, Baidu, Yandex, Yahoo, DuckDuckGo, eBay, Walmart, YouTube, App Stores, and [more](https://serpapi.com).

Query a vast range of data at scale, including web search results, flight schedules, stock market data, news headlines, and [more](https://serpapi.com).

## Features

- `async`-first → non-blocking API with full `CancellationToken` support, plus sync convenience wrappers
- `IAsyncEnumerable` pagination → stream every page of results with one loop
- Dependency injection → `IHttpClientFactory` integration for ASP.NET Core / generic host
- Broad reach → targets .NET Standard 2.0 and .NET 7, 8, 9, 10
- Zero external runtime dependencies on modern .NET
- Extensive documentation and real-world examples included throughout

## Installation

.NET 7 and higher are supported, plus .NET Framework 4.6.1+ via .NET Standard 2.0. Check [Supported .NET versions](#supported-net-versions) for the full matrix.

If you are upgrading from the legacy [google-search-results-dotnet](https://github.com/serpapi/google-search-results-dotnet) library, check our [migration guide](#migration-quick-guide).

```bash
dotnet add package serpapi
```

[NuGet package page](https://www.nuget.org/packages/serpapi)

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

This example runs a search for "coffee" on Google Light. See the [playground](https://serpapi.com/playground) to generate your own code.

The SerpApi key can be obtained from [serpapi.com/signup](https://serpapi.com/users/sign_up?plan=free).

Environment variables are a secure, safe, and easy way to manage secrets.
Set `export SERPAPI_KEY=<secret_serpapi_key>` in your shell.
.NET accesses these variables via `Environment.GetEnvironmentVariable("SERPAPI_KEY")`.

## Search API advanced usage with Google search engine

This example dives into the available parameters for the Google search engine.
The list of parameters depends on the chosen search engine.

```csharp
using SerpApi;

// serpapi client created with an API key and optional configuration
using var client = new SerpApiClient(
    Environment.GetEnvironmentVariable("SERPAPI_KEY")!,
    new SerpApiClientOptions
    {
        Timeout = TimeSpan.FromSeconds(30) // HTTP timeout (default: 60s)
    });

// search query overview (more fields available depending on search engine)
var parameters = new Dictionary<string, string>
{
    // select the search engine (full list: https://serpapi.com/)
    ["engine"] = "google",
    // actual search query
    ["q"] = "Coffee",
    // then add search engine specific options.
    // for example: google specific parameters: https://serpapi.com/search-api
    ["google_domain"] = "google.com",
    ["location"] = "Austin, Texas",   // see: Location API
    ["device"] = "desktop",           // desktop|mobile|tablet
    ["hl"] = "en",                    // Google UI language
    ["gl"] = "us",                    // Google country
    ["safe"] = "active",              // safe search flag
    ["start"] = "0",                  // pagination offset
    ["num"] = "10"                    // number of results
};

// search results as a parsed response
using var results = await client.SearchAsync(parameters);
Console.WriteLine(results.SearchId);
Console.WriteLine(results.OrganicResults);
Console.WriteLine(results["local_results"]);

// search results as a raw HTML string
string rawHtml = await client.HtmlAsync(parameters);
```

→ [SerpApi documentation](https://serpapi.com/search-api).

### Documentations

- [Full documentation on SerpApi.com](https://serpapi.com)
- [Library GitHub page](https://github.com/serpapi/serpapi-dotnet)
- [Library NuGet page](https://www.nuget.org/packages/serpapi)
- [API health status](https://serpapi.com/status)

## Advanced search API usage

### Search concurrently

A single `SerpApiClient` can run many searches at once — no thread pool or connection juggling required.

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

## APIs supported

### Location API

```csharp
var locations = await client.LocationAsync("Austin, TX", limit: 3);
foreach (var loc in locations.EnumerateArray())
{
    Console.WriteLine(loc.GetProperty("name").GetString());
}
```

NOTE: api_key is not required for this endpoint.

### Search Archive API

This API allows retrieving previous search results (free of charge).
First, run a search and save the search ID; then fetch it back from the archive.

```csharp
using var results = await client.SearchAsync(parameters);
string searchId = results.SearchId!;

using var archived = await client.SearchArchiveAsync(searchId);
```

### Account API

```csharp
using var account = await client.AccountAsync();
Console.WriteLine(account["plan_id"]);
```

It prints your account information: plan, searches left, usage this month, and more.

## Basic example per search engine

### Search google

```csharp
using var results = await client.SearchAsync(new Dictionary<string, string>
{
    ["engine"] = "google",
    ["q"] = "coffee",
    ["location"] = "Austin, Texas"
});
```

see: [https://serpapi.com/search-api](https://serpapi.com/search-api)

### Search google light

```csharp
using var results = await client.SearchAsync(new Dictionary<string, string>
{
    ["engine"] = "google_light",
    ["q"] = "coffee"
});
```

see: [https://serpapi.com/google-light-api](https://serpapi.com/google-light-api)

### Search google scholar

```csharp
using var results = await client.SearchAsync(new Dictionary<string, string>
{
    ["engine"] = "google_scholar",
    ["q"] = "machine learning"
});
```

see: [https://serpapi.com/google-scholar-api](https://serpapi.com/google-scholar-api)

### Search google news

```csharp
using var results = await client.SearchAsync(new Dictionary<string, string>
{
    ["engine"] = "google_news",
    ["q"] = "artificial intelligence"
});
```

see: [https://serpapi.com/google-news-api](https://serpapi.com/google-news-api)

### Search google maps

```csharp
using var results = await client.SearchAsync(new Dictionary<string, string>
{
    ["engine"] = "google_maps",
    ["q"] = "pizza",
    ["ll"] = "@40.7455096,-74.0083012,14z"
});
```

see: [https://serpapi.com/google-maps-api](https://serpapi.com/google-maps-api)

### Search google shopping

```csharp
using var results = await client.SearchAsync(new Dictionary<string, string>
{
    ["engine"] = "google_shopping",
    ["q"] = "laptop"
});
```

see: [https://serpapi.com/google-shopping-api](https://serpapi.com/google-shopping-api)

### Search google jobs

```csharp
using var results = await client.SearchAsync(new Dictionary<string, string>
{
    ["engine"] = "google_jobs",
    ["q"] = "software engineer"
});
```

see: [https://serpapi.com/google-jobs-api](https://serpapi.com/google-jobs-api)

### Search google images

```csharp
using var results = await client.SearchAsync(new Dictionary<string, string>
{
    ["engine"] = "google_images",
    ["q"] = "sunset"
});
```

see: [https://serpapi.com/images-results](https://serpapi.com/images-results)

### Search google finance

```csharp
using var results = await client.SearchAsync(new Dictionary<string, string>
{
    ["engine"] = "google_finance",
    ["q"] = "AAPL:NASDAQ"
});
```

see: [https://serpapi.com/google-finance-api](https://serpapi.com/google-finance-api)

### Search bing

```csharp
using var results = await client.SearchAsync(new Dictionary<string, string>
{
    ["engine"] = "bing",
    ["q"] = "coffee"
});
```

see: [https://serpapi.com/bing-search-api](https://serpapi.com/bing-search-api)

### Search duckduckgo

```csharp
using var results = await client.SearchAsync(new Dictionary<string, string>
{
    ["engine"] = "duckduckgo",
    ["q"] = "coffee"
});
```

see: [https://serpapi.com/duckduckgo-search-api](https://serpapi.com/duckduckgo-search-api)

### Search baidu

```csharp
using var results = await client.SearchAsync(new Dictionary<string, string>
{
    ["engine"] = "baidu",
    ["q"] = "coffee"
});
```

see: [https://serpapi.com/baidu-search-api](https://serpapi.com/baidu-search-api)

### Search yahoo

```csharp
using var results = await client.SearchAsync(new Dictionary<string, string>
{
    ["engine"] = "yahoo",
    ["p"] = "coffee"
});
```

see: [https://serpapi.com/yahoo-search-api](https://serpapi.com/yahoo-search-api)

### Search youtube

```csharp
using var results = await client.SearchAsync(new Dictionary<string, string>
{
    ["engine"] = "youtube",
    ["search_query"] = "latte art"
});
```

see: [https://serpapi.com/youtube-search-api](https://serpapi.com/youtube-search-api)

### Search walmart

```csharp
using var results = await client.SearchAsync(new Dictionary<string, string>
{
    ["engine"] = "walmart",
    ["query"] = "coffee maker"
});
```

see: [https://serpapi.com/walmart-search-api](https://serpapi.com/walmart-search-api)

### Search ebay

```csharp
using var results = await client.SearchAsync(new Dictionary<string, string>
{
    ["engine"] = "ebay",
    ["_nkw"] = "laptop"
});
```

see: [https://serpapi.com/ebay-search-api](https://serpapi.com/ebay-search-api)

### Search amazon

```csharp
using var results = await client.SearchAsync(new Dictionary<string, string>
{
    ["engine"] = "amazon",
    ["k"] = "coffee"
});
```

see: [https://serpapi.com/amazon-search-api](https://serpapi.com/amazon-search-api)

### Search naver

```csharp
using var results = await client.SearchAsync(new Dictionary<string, string>
{
    ["engine"] = "naver",
    ["query"] = "coffee"
});
```

see: [https://serpapi.com/naver-search-api](https://serpapi.com/naver-search-api)

### Search apple app store

```csharp
using var results = await client.SearchAsync(new Dictionary<string, string>
{
    ["engine"] = "apple_app_store",
    ["term"] = "coffee"
});
```

see: [https://serpapi.com/apple-app-store](https://serpapi.com/apple-app-store)

### Search home depot

```csharp
using var results = await client.SearchAsync(new Dictionary<string, string>
{
    ["engine"] = "home_depot",
    ["q"] = "drill"
});
```

see: [https://serpapi.com/home-depot-search-api](https://serpapi.com/home-depot-search-api)

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

## Migration quick guide

If you were already using the [google-search-results-dotnet](https://github.com/serpapi/google-search-results-dotnet) package, here are the changes. It's a new package ID — install `serpapi` instead; there's no automatic upgrade path.

```csharp
// define a search
// old way: one class per engine (GoogleSearch, BingSearch, BaiduSearch, ...)
var ht = new Hashtable { { "q", "coffee" } };
GoogleSearch search = new GoogleSearch(ht, apiKey);
// new way: one client for every engine, selected via the "engine" parameter
using var client = new SerpApiClient(apiKey);
var parameters = new Dictionary<string, string> { ["engine"] = "google", ["q"] = "coffee" };

// search returns JSON
// old way (synchronous, Newtonsoft JObject)
JObject data = search.GetJson();
// new way (async, System.Text.Json based SerpApiResponse)
using var results = await client.SearchAsync(parameters);

// search returns raw HTML
// old way
string html = search.GetHtml();
// new way
string html = await client.HtmlAsync(parameters);

// other methods: the Get prefix is removed, Async suffix added
// old -> new way
// search.GetSearchArchiveJson(id) -> await client.SearchArchiveAsync(id)
// search.GetAccount()             -> await client.AccountAsync()
// search.GetLocation(q, limit)    -> await client.LocationAsync(q, limit)

// timeout
// old way
search.setTimeoutSeconds(30);
// new way (at construction)
using var client = new SerpApiClient(apiKey, new SerpApiClientOptions { Timeout = TimeSpan.FromSeconds(30) });

// cleanup
// old way
search.Close();
// new way: the client implements IDisposable
// (and each SerpApiResponse is IDisposable too)
```

Most notable improvements:
- Async-first API with `CancellationToken` support (sync wrappers still available).
- `System.Text.Json` instead of `Newtonsoft.Json` — zero external dependencies on modern .NET.
- Typed errors: `SerpApiKeyException`, `SerpApiHttpException`, `SerpApiTimeoutException`, `SerpApiException` (was a single `SerpApiSearchException`).
- Built-in pagination: `NextPageAsync(response)` and `SearchPagesAsync(parameters)` (`IAsyncEnumerable`).
- Dependency injection via `services.AddSerpApi(...)`.

## Supported .NET versions

| Target framework | Minimum consumer runtime | Notes |
|---|---|---|
| `netstandard2.0` | .NET Framework 4.6.1+, .NET Core 2.0+, Mono, Xamarin, UWP | Ships `Microsoft.Bcl.AsyncInterfaces` and `System.Text.Json` as polyfills |
| `net7.0` | .NET 7 | Out of support upstream, still built and tested |
| `net8.0` | .NET 8 (LTS) | |
| `net9.0` | .NET 9 (STS) | |
| `net10.0` | .NET 10 (LTS) | Used for `dotnet pack` and local dev builds |

CI builds and tests every target framework above on both **Linux** and **Windows**. Building locally requires the [.NET 10 SDK](https://dotnet.microsoft.com/download) (a single SDK at or above the highest TFM can build all lower ones).

## Contributing

Bug reports and pull requests are welcome on GitHub at https://github.com/serpapi/serpapi-dotnet. See [Supported .NET versions](#supported-net-versions) for SDK requirements.

```bash
git clone https://github.com/serpapi/serpapi-dotnet.git
cd serpapi-dotnet
dotnet build
dotnet test
```

## License

MIT — see [LICENSE](LICENSE).
