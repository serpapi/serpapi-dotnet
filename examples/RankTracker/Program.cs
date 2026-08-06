// Rank Tracker: monitor keyword positions page by page.
// Use case: SEO teams tracking if a site appears in top 30 for target keywords.
// Demonstrates pagination to scan multiple result pages.

using SerpApi;
using System.Text.Json;

var apiKey = args.Length > 0 ? args[0] : Environment.GetEnvironmentVariable("SERPAPI_KEY");
if (string.IsNullOrEmpty(apiKey))
{
    Console.WriteLine("Usage: dotnet run -- <API_KEY>");
    Console.WriteLine("   or: SERPAPI_KEY=... dotnet run");
    return;
}

using var client = new SerpApiClient(apiKey);

var targetDomain = "serpapi.com";
var keywords = new[] { "search api", "serp scraping", "google search api" };

Console.WriteLine($"Tracking rankings for: {targetDomain}\n");

foreach (var keyword in keywords)
{
    Console.Write($"  \"{keyword}\" ... ");

    var found = false;
    var position = 0;

    await foreach (var page in client.SearchPagesAsync(
        new Dictionary<string, string>
        {
            ["engine"] = "google_light",
            ["q"] = keyword,
            ["num"] = "10"
        },
        maxPages: 3))
    {
        using (page)
        {
            if (page.OrganicResults is not { } results) continue;

            foreach (var result in results.EnumerateArray())
            {
                position++;
                var link = result.TryGetProperty("link", out var l) ? l.GetString() ?? "" : "";
                if (link.Contains(targetDomain, StringComparison.OrdinalIgnoreCase))
                {
                    Console.WriteLine($"#{position}");
                    found = true;
                    break;
                }
            }
        }

        if (found) break;
    }

    if (!found)
        Console.WriteLine($"Not in top {position}");
}
