// Competitor Tracker: monitor your brand vs competitors across search engines.
// Use case: Marketing teams tracking SERP visibility for key terms.
// Runs searches across Google and Bing, reports who ranks where.

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

var keyword = "project management software";
var trackedDomains = new[] { "asana.com", "monday.com", "clickup.com", "notion.so" };

Console.WriteLine($"Tracking: \"{keyword}\"");
Console.WriteLine($"Domains: {string.Join(", ", trackedDomains)}\n");

// Search Google and Bing in parallel
var google = client.SearchAsync(new Dictionary<string, string>
{
    ["engine"] = "google_light",
    ["q"] = keyword,
    ["num"] = "20"
});

var bing = client.SearchAsync(new Dictionary<string, string>
{
    ["engine"] = "bing",
    ["q"] = keyword,
    ["count"] = "20"
});

try
{
    await Task.WhenAll(google, bing);

    var engines = new[] { ("Google", await google), ("Bing", await bing) };
    foreach (var (engineName, response) in engines)
    {
        Console.WriteLine($"=== {engineName} Rankings ===");

        var organic = response.OrganicResults;
        if (organic is not { } results)
        {
            Console.WriteLine("  No results\n");
            continue;
        }

        foreach (var domain in trackedDomains)
        {
            var position = -1;
            var idx = 0;
            foreach (var r in results.EnumerateArray())
            {
                idx++;
                var link = r.TryGetProperty("link", out var l) ? l.GetString() ?? "" : "";
                if (link.Contains(domain, StringComparison.OrdinalIgnoreCase))
                {
                    position = idx;
                    break;
                }
            }

            var status = position > 0 ? $"#{position}" : "Not in top 20";
            Console.WriteLine($"  {domain,-20} {status}");
        }
        Console.WriteLine();
    }
}
catch (SerpApiException ex)
{
    Console.WriteLine($"Search error: {ex.Message}");
}
finally
{
    DisposeCompleted(google);
    DisposeCompleted(bing);
}

static void DisposeCompleted(Task<SerpApiResponse> task)
{
    if (task.Status == TaskStatus.RanToCompletion)
        task.Result.Dispose();
}
