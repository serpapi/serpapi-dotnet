using System.Text.Json;
using Xunit;

namespace SerpApi.Tests;

/// <summary>
/// Integration tests that hit the real SerpApi.
/// Skipped at runtime unless SERPAPI_API_KEY env var is set.
/// Run with: dotnet test --filter "Category=Integration"
/// </summary>
[Trait("Category", "Integration")]
public class IntegrationTests
{
    private static readonly string? ApiKey = Environment.GetEnvironmentVariable("SERPAPI_API_KEY");

    private static SerpApiClient CreateClient()
    {
        Skip.If(string.IsNullOrEmpty(ApiKey), "SERPAPI_API_KEY not set");
        return new SerpApiClient(ApiKey!);
    }

    [SkippableFact]
    public async Task Google_ReturnsOrganicResults()
    {
        using var client = CreateClient();
        var result = await client.SearchAsync(new Dictionary<string, string>
        {
            ["engine"] = "google",
            ["q"] = "coffee",
            ["location"] = "Austin, Texas"
        });

        Assert.NotNull(result.SearchMetadata);
        Assert.NotNull(result.SearchId);
        Assert.NotNull(result.OrganicResults);
        Assert.True(result.OrganicResults!.Value.GetArrayLength() > 0);

        var first = result.OrganicResults!.Value[0];
        Assert.True(first.TryGetProperty("title", out _));
        Assert.True(first.TryGetProperty("link", out _));
    }

    [SkippableFact]
    public async Task Google_HtmlEndpoint_ReturnsHtml()
    {
        using var client = CreateClient();
        var html = await client.HtmlAsync(new Dictionary<string, string>
        {
            ["engine"] = "google",
            ["q"] = "coffee"
        });

        Assert.Contains("</", html);
        Assert.True(html.Length > 100);
    }

    [SkippableFact]
    public async Task Bing_ReturnsOrganicResults()
    {
        using var client = CreateClient();
        var result = await client.SearchAsync(new Dictionary<string, string>
        {
            ["engine"] = "bing",
            ["q"] = "coffee"
        });

        Assert.NotNull(result.OrganicResults);
        Assert.True(result.OrganicResults!.Value.GetArrayLength() > 0);
    }

    [SkippableFact]
    public async Task GoogleMaps_ReturnsLocalResults()
    {
        using var client = CreateClient();
        var result = await client.SearchAsync(new Dictionary<string, string>
        {
            ["engine"] = "google_maps",
            ["q"] = "pizza",
            ["ll"] = "@40.7455096,-74.0083012,15.1z",
            ["type"] = "search"
        });

        var localResults = result["local_results"];
        Assert.NotNull(localResults);
        Assert.True(localResults!.Value.GetArrayLength() > 0);
    }

    [SkippableFact]
    public async Task YouTube_ReturnsVideoResults()
    {
        using var client = CreateClient();
        var result = await client.SearchAsync(new Dictionary<string, string>
        {
            ["engine"] = "youtube",
            ["search_query"] = "coffee"
        });

        var videoResults = result["video_results"];
        Assert.NotNull(videoResults);
        Assert.True(videoResults!.Value.GetArrayLength() > 0);
    }

    [SkippableFact]
    public async Task Location_ReturnsResults()
    {
        using var client = CreateClient();
        var result = await client.LocationAsync("Austin, TX", limit: 3);

        Assert.Equal(JsonValueKind.Array, result.ValueKind);
        Assert.True(result.GetArrayLength() > 0);

        var first = result[0];
        Assert.True(first.TryGetProperty("name", out _));
        Assert.True(first.TryGetProperty("google_id", out _));
    }

    [SkippableFact]
    public async Task Account_ReturnsAccountInfo()
    {
        using var client = CreateClient();
        var result = await client.AccountAsync();

        Assert.NotNull(result["account_id"]);
        Assert.NotNull(result["api_key"]);
    }

    [SkippableFact]
    public async Task ArchiveRoundTrip_SearchThenRetrieve()
    {
        using var client = CreateClient();

        var search = await client.SearchAsync(new Dictionary<string, string>
        {
            ["engine"] = "google",
            ["q"] = "serpapi test archive"
        });

        var searchId = search.SearchId;
        Assert.NotNull(searchId);

        var archived = await client.SearchArchiveAsync(searchId!);
        Assert.NotNull(archived.SearchId);
        Assert.Equal(searchId, archived.SearchId);
    }

    [SkippableFact]
    public async Task Pagination_NextPageWorks()
    {
        using var client = CreateClient();

        var firstPage = await client.SearchAsync(new Dictionary<string, string>
        {
            ["engine"] = "google",
            ["q"] = "coffee shops",
            ["num"] = "10"
        });

        Assert.NotNull(firstPage.NextPageUrl);

        var secondPage = await client.NextPageAsync(firstPage);
        Assert.NotNull(secondPage);
        Assert.NotNull(secondPage!.OrganicResults);
        Assert.True(secondPage.OrganicResults!.Value.GetArrayLength() > 0);
    }

    [SkippableFact]
    public async Task SearchPagesAsync_IteratesMultiplePages()
    {
        using var client = CreateClient();

        var pages = new List<SerpApiResponse>();
        await foreach (var page in client.SearchPagesAsync(
            new Dictionary<string, string>
            {
                ["engine"] = "google",
                ["q"] = "best coffee beans",
                ["num"] = "10"
            },
            maxPages: 3))
        {
            pages.Add(page);
        }

        Assert.True(pages.Count >= 2, $"Expected at least 2 pages, got {pages.Count}");
        foreach (var page in pages)
            page.Dispose();
    }
}
