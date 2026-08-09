using System.Text.Json;
using Xunit;

namespace SerpApi.Tests;

/// <summary>
/// Integration tests that hit the real SerpApi.
/// Skipped at runtime unless SERPAPI_KEY env var is set.
/// Run with: dotnet test --filter "Category=Integration"
///
/// Uses async=true + no_cache=true: fires all searches concurrently at fixture init,
/// polls results in parallel. Each test asserts on pre-fetched results (< 1ms).
/// Slow tests (pagination, HTML) are in separate classes for xUnit parallelism.
/// </summary>
public class IntegrationFixture : IAsyncLifetime
{
    public SerpApiClient? Client { get; private set; }
    public Dictionary<string, SerpApiResponse> Results { get; } = new();

    private static readonly Dictionary<string, Dictionary<string, string>> Searches = new()
    {
        ["google"] = new() { ["engine"] = "google", ["q"] = "coffee", ["location"] = "Austin, Texas" },
        ["google_archive"] = new() { ["engine"] = "google", ["q"] = "serpapi test archive" },
        ["bing"] = new() { ["engine"] = "bing", ["q"] = "coffee" },
        ["google_maps"] = new() { ["engine"] = "google_maps", ["q"] = "pizza", ["ll"] = "@40.7455096,-74.0083012,15.1z", ["type"] = "search" },
        ["youtube"] = new() { ["engine"] = "youtube", ["search_query"] = "coffee" },
        ["google_shopping"] = new() { ["engine"] = "google_shopping", ["q"] = "coffee maker" },
        ["google_news"] = new() { ["engine"] = "google_news", ["q"] = "technology" },
        ["google_scholar"] = new() { ["engine"] = "google_scholar", ["q"] = "machine learning" },
        ["google_jobs"] = new() { ["engine"] = "google_jobs", ["q"] = "software engineer" },
        ["duckduckgo"] = new() { ["engine"] = "duckduckgo", ["q"] = "coffee" },
        ["yahoo"] = new() { ["engine"] = "yahoo", ["p"] = "coffee" },
        ["baidu"] = new() { ["engine"] = "baidu", ["q"] = "coffee" },
        ["yandex"] = new() { ["engine"] = "yandex", ["text"] = "coffee" },
        ["apple_app_store"] = new() { ["engine"] = "apple_app_store", ["term"] = "weather" },
        ["walmart"] = new() { ["engine"] = "walmart", ["query"] = "coffee" },
        ["ebay"] = new() { ["engine"] = "ebay", ["_nkw"] = "coffee" },
        ["naver"] = new() { ["engine"] = "naver", ["query"] = "coffee" },
        ["google_images"] = new() { ["engine"] = "google_images", ["q"] = "coffee", ["tbm"] = "isch" },
        ["google_events"] = new() { ["engine"] = "google_events", ["q"] = "coffee" },
        ["google_autocomplete"] = new() { ["engine"] = "google_autocomplete", ["q"] = "coffee" },
        ["google_local_services"] = new() { ["engine"] = "google_local_services", ["q"] = "electrician", ["data_cid"] = "6745062158417646970" },
        ["google_reverse_image"] = new() { ["engine"] = "google_reverse_image", ["image_url"] = "https://i.imgur.com/HBrB8p0.png" },
        ["google_play"] = new() { ["engine"] = "google_play", ["q"] = "kite", ["store"] = "apps" },
        ["home_depot"] = new() { ["engine"] = "home_depot", ["q"] = "table" },
    };

    public async Task InitializeAsync()
    {
        var apiKey = Environment.GetEnvironmentVariable("SERPAPI_KEY");
        if (string.IsNullOrEmpty(apiKey)) return;

        Client = new SerpApiClient(apiKey);

        var searchIds = new Dictionary<string, string>();
        var fireTasks = Searches.Select(async kv =>
        {
            try
            {
                var parameters = new Dictionary<string, string>(kv.Value)
                {
                    ["async"] = "true",
                    ["no_cache"] = "true"
                };
                var queued = await Client.SearchAsync(parameters);
                lock (searchIds) { searchIds[kv.Key] = queued.SearchId!; }
                queued.Dispose();
            }
            catch
            {
                // Individual engine failure should not crash the fixture
            }
        });
        await Task.WhenAll(fireTasks);

        var pollTasks = searchIds.Select(async kv =>
        {
            try
            {
                for (var i = 0; i < 240; i++)
                {
                    await Task.Delay(250);
                    var result = await Client.SearchArchiveAsync(kv.Value);
                    var status = result.SearchMetadata?.GetProperty("status").GetString();
                    if (status == "Success")
                    {
                        lock (Results) { Results[kv.Key] = result; }
                        return;
                    }
                    result.Dispose();
                }
            }
            catch
            {
                // Individual engine poll failure should not crash the fixture
            }
        });
        await Task.WhenAll(pollTasks);
    }

    public Task DisposeAsync()
    {
        foreach (var r in Results.Values) r.Dispose();
        Client?.Dispose();
        return Task.CompletedTask;
    }
}

// --- Prefetched search tests (all < 1ms, share one fixture) ---

[Trait("Category", "Integration")]
public class SearchIntegrationTests : IClassFixture<IntegrationFixture>
{
    private readonly IntegrationFixture _f;
    public SearchIntegrationTests(IntegrationFixture fixture) => _f = fixture;

    private SerpApiResponse GetResult(string key)
    {
        Skip.If(_f.Client is null, "SERPAPI_KEY not set");
        Skip.If(!_f.Results.ContainsKey(key), $"Engine '{key}' not available");
        return _f.Results[key];
    }

    [SkippableFact]
    public void Google_ReturnsOrganicResults()
    {
        var result = GetResult("google");

        Assert.NotNull(result.SearchMetadata);
        Assert.NotNull(result.SearchId);
        Assert.NotNull(result.OrganicResults);
        Assert.True(result.OrganicResults!.Value.GetArrayLength() > 0);

        var first = result.OrganicResults!.Value[0];
        Assert.True(first.TryGetProperty("title", out _));
        Assert.True(first.TryGetProperty("link", out _));
    }

    [SkippableFact]
    public void Bing_ReturnsOrganicResults()
    {
        var result = GetResult("bing");
        Assert.NotNull(result.OrganicResults);
        Assert.True(result.OrganicResults!.Value.GetArrayLength() > 0);
    }

    [SkippableFact]
    public void GoogleMaps_ReturnsLocalResults()
    {
        var result = GetResult("google_maps");
        var localResults = result["local_results"];
        Assert.NotNull(localResults);
        Assert.True(localResults!.Value.GetArrayLength() > 0);
    }

    [SkippableFact]
    public void YouTube_ReturnsVideoResults()
    {
        var result = GetResult("youtube");
        var videoResults = result["video_results"];
        Assert.NotNull(videoResults);
        Assert.True(videoResults!.Value.GetArrayLength() > 0);
    }

    [SkippableFact]
    public async Task ArchiveRoundTrip_SearchThenRetrieve()
    {
        var original = GetResult("google_archive");
        var searchId = original.SearchId;
        Assert.NotNull(searchId);

        var archived = await _f.Client!.SearchArchiveAsync(searchId!);
        Assert.NotNull(archived.SearchId);
        Assert.Equal(searchId, archived.SearchId);
    }

    [SkippableFact]
    public void GoogleShopping_ReturnsShoppingResults()
    {
        var result = GetResult("google_shopping");
        var shoppingResults = result["shopping_results"];
        Assert.NotNull(shoppingResults);
        Assert.True(shoppingResults!.Value.GetArrayLength() > 0);
    }

    [SkippableFact]
    public void GoogleNews_ReturnsNewsResults()
    {
        var result = GetResult("google_news");
        var newsResults = result["news_results"];
        Assert.NotNull(newsResults);
        Assert.True(newsResults!.Value.GetArrayLength() > 0);
    }

    [SkippableFact]
    public void GoogleScholar_ReturnsOrganicResults()
    {
        var result = GetResult("google_scholar");
        Assert.NotNull(result.OrganicResults);
        Assert.True(result.OrganicResults!.Value.GetArrayLength() > 0);
    }

    [SkippableFact]
    public void GoogleJobs_ReturnsJobsResults()
    {
        var result = GetResult("google_jobs");
        var jobsResults = result["jobs_results"];
        Assert.NotNull(jobsResults);
        Assert.True(jobsResults!.Value.GetArrayLength() > 0);
    }

    [SkippableFact]
    public void DuckDuckGo_ReturnsOrganicResults()
    {
        var result = GetResult("duckduckgo");
        Assert.NotNull(result.OrganicResults);
        Assert.True(result.OrganicResults!.Value.GetArrayLength() > 0);
    }

    [SkippableFact]
    public void Yahoo_ReturnsOrganicResults()
    {
        var result = GetResult("yahoo");
        Assert.NotNull(result.OrganicResults);
        Assert.True(result.OrganicResults!.Value.GetArrayLength() > 0);
    }

    [SkippableFact]
    public void Baidu_ReturnsOrganicResults()
    {
        var result = GetResult("baidu");
        Assert.NotNull(result.OrganicResults);
        Assert.True(result.OrganicResults!.Value.GetArrayLength() > 0);
    }

    [SkippableFact]
    public void Yandex_ReturnsOrganicResults()
    {
        var result = GetResult("yandex");
        Assert.NotNull(result.OrganicResults);
        Assert.True(result.OrganicResults!.Value.GetArrayLength() > 0);
    }

    [SkippableFact]
    public void AppleAppStore_ReturnsOrganicResults()
    {
        var result = GetResult("apple_app_store");
        Assert.NotNull(result.OrganicResults);
        Assert.True(result.OrganicResults!.Value.GetArrayLength() > 0);
    }

    [SkippableFact]
    public void Walmart_ReturnsOrganicResults()
    {
        var result = GetResult("walmart");
        Assert.NotNull(result.OrganicResults);
        Assert.True(result.OrganicResults!.Value.GetArrayLength() > 0);
    }

    [SkippableFact]
    public void Ebay_ReturnsOrganicResults()
    {
        var result = GetResult("ebay");
        Assert.NotNull(result.OrganicResults);
        Assert.True(result.OrganicResults!.Value.GetArrayLength() > 0);
    }

    [SkippableFact]
    public void Naver_ReturnsResults()
    {
        var result = GetResult("naver");
        var adsResults = result["ads_results"];
        Assert.NotNull(adsResults);
        Assert.True(adsResults!.Value.GetArrayLength() > 0);
    }

    [SkippableFact]
    public void GoogleImages_ReturnsImageResults()
    {
        var result = GetResult("google_images");
        var imageResults = result["images_results"];
        Assert.NotNull(imageResults);
        Assert.True(imageResults!.Value.GetArrayLength() > 0);
    }

    [SkippableFact]
    public void GoogleEvents_ReturnsEventResults()
    {
        var result = GetResult("google_events");
        var eventsResults = result["events_results"];
        Assert.NotNull(eventsResults);
        Assert.True(eventsResults!.Value.GetArrayLength() > 0);
    }

    [SkippableFact]
    public void GoogleAutocomplete_ReturnsSuggestions()
    {
        var result = GetResult("google_autocomplete");
        var suggestions = result["suggestions"];
        Assert.NotNull(suggestions);
        Assert.True(suggestions!.Value.GetArrayLength() > 0);
    }

    [SkippableFact]
    public void GoogleLocalServices_ReturnsLocalAds()
    {
        var result = GetResult("google_local_services");
        var localAds = result["local_ads"];
        Assert.NotNull(localAds);
        Assert.True(localAds!.Value.GetArrayLength() > 0);
    }

    [SkippableFact]
    public void GoogleReverseImage_ReturnsImageResults()
    {
        var result = GetResult("google_reverse_image");
        var imageResults = result["image_results"];
        Assert.NotNull(imageResults);
    }

    [SkippableFact]
    public void GooglePlay_ReturnsOrganicResults()
    {
        var result = GetResult("google_play");
        Assert.NotNull(result.OrganicResults);
        Assert.True(result.OrganicResults!.Value.GetArrayLength() > 0);
    }

    [SkippableFact]
    public void HomeDepot_ReturnsProducts()
    {
        var result = GetResult("home_depot");
        var products = result["products"];
        Assert.NotNull(products);
        Assert.True(products!.Value.GetArrayLength() > 0);
    }
}

// --- Each slow test in its own class for parallel execution ---

[Trait("Category", "Integration")]
public class HtmlIntegrationTest
{
    [SkippableFact]
    public async Task Google_HtmlEndpoint_ReturnsHtml()
    {
        var apiKey = Environment.GetEnvironmentVariable("SERPAPI_KEY");
        Skip.If(string.IsNullOrEmpty(apiKey), "SERPAPI_KEY not set");

        using var client = new SerpApiClient(apiKey!);
        var html = await client.HtmlAsync(new Dictionary<string, string>
        {
            ["engine"] = "google",
            ["q"] = "coffee",
            ["no_cache"] = "true"
        });

        Assert.Contains("</", html);
        Assert.True(html.Length > 100);
    }
}

[Trait("Category", "Integration")]
public class PaginationNextPageTest
{
    [SkippableFact]
    public async Task Pagination_NextPageWorks()
    {
        var apiKey = Environment.GetEnvironmentVariable("SERPAPI_KEY");
        Skip.If(string.IsNullOrEmpty(apiKey), "SERPAPI_KEY not set");

        using var client = new SerpApiClient(apiKey!);
        var firstPage = await client.SearchAsync(new Dictionary<string, string>
        {
            ["engine"] = "google",
            ["q"] = "coffee shops",
            ["num"] = "10",
            ["no_cache"] = "true"
        });

        Assert.NotNull(firstPage.NextPageUrl);

        var secondPage = await client.NextPageAsync(firstPage);
        Assert.NotNull(secondPage);
        Assert.NotNull(secondPage!.OrganicResults);
        Assert.True(secondPage.OrganicResults!.Value.GetArrayLength() > 0);
    }
}

[Trait("Category", "Integration")]
public class PaginationIteratorTest
{
    [SkippableFact]
    public async Task SearchPagesAsync_IteratesMultiplePages()
    {
        var apiKey = Environment.GetEnvironmentVariable("SERPAPI_KEY");
        Skip.If(string.IsNullOrEmpty(apiKey), "SERPAPI_KEY not set");

        using var client = new SerpApiClient(apiKey!);
        var pages = new List<SerpApiResponse>();
        await foreach (var page in client.SearchPagesAsync(
            new Dictionary<string, string>
            {
                ["engine"] = "google",
                ["q"] = "best coffee beans",
                ["num"] = "10",
                ["no_cache"] = "true"
            },
            maxPages: 2))
        {
            pages.Add(page);
        }

        Assert.True(pages.Count >= 2, $"Expected at least 2 pages, got {pages.Count}");
        foreach (var page in pages)
            page.Dispose();
    }
}

[Trait("Category", "Integration")]
public class LocationIntegrationTest
{
    [SkippableFact]
    public async Task Location_ReturnsResults()
    {
        var apiKey = Environment.GetEnvironmentVariable("SERPAPI_KEY");
        Skip.If(string.IsNullOrEmpty(apiKey), "SERPAPI_KEY not set");

        using var client = new SerpApiClient(apiKey!);
        var result = await client.LocationAsync("Austin, TX", limit: 3);

        Assert.Equal(JsonValueKind.Array, result.ValueKind);
        Assert.True(result.GetArrayLength() > 0);

        var first = result[0];
        Assert.True(first.TryGetProperty("name", out _));
        Assert.True(first.TryGetProperty("google_id", out _));
    }
}

[Trait("Category", "Integration")]
public class AccountIntegrationTest
{
    [SkippableFact]
    public async Task Account_ReturnsAccountInfo()
    {
        var apiKey = Environment.GetEnvironmentVariable("SERPAPI_KEY");
        Skip.If(string.IsNullOrEmpty(apiKey), "SERPAPI_KEY not set");

        using var client = new SerpApiClient(apiKey!);
        var result = await client.AccountAsync();

        Assert.NotNull(result["account_id"]);
        Assert.NotNull(result["api_key"]);
    }
}
