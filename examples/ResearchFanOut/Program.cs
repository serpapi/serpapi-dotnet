// Research Fan-Out: one question → multiple engines in parallel.
// Demonstrates: Task.WhenAll, safe disposal, partial failure handling.
// Runs 4 API calls concurrently.

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
var query = args.Length > 1 ? args[1] : "coffee brewing methods";

Console.WriteLine($"Researching: {query}\n");

// Fan-out: same topic, different engines, all in parallel.
var tasks = new (string Name, Task<SerpApiResponse> Call)[]
{
    ("Web", client.SearchAsync(new Dictionary<string, string>
    {
        ["engine"] = "google_light",
        ["q"] = query
    })),
    ("News", client.SearchAsync(new Dictionary<string, string>
    {
        ["engine"] = "google_news_light",
        ["q"] = query
    })),
    ("Scholar", client.SearchAsync(new Dictionary<string, string>
    {
        ["engine"] = "google_scholar",
        ["q"] = query
    })),
    ("Bing", client.SearchAsync(new Dictionary<string, string>
    {
        ["engine"] = "bing",
        ["q"] = query
    }))
};

// Await all — handle partial failures gracefully.
await Task.WhenAll(tasks.Select(t => t.Call));

try
{
    foreach (var (name, call) in tasks)
    {
        if (call.IsFaulted)
        {
            Console.WriteLine($"=== {name}: FAILED ({call.Exception?.InnerException?.Message}) ===\n");
            continue;
        }

        var response = call.Result;
        var results = response.OrganicResults
            ?? response["news_results"];

        if (results is not { } arr)
        {
            Console.WriteLine($"=== {name}: no results ===\n");
            continue;
        }

        Console.WriteLine($"=== {name} ({arr.GetArrayLength()} results) ===");
        foreach (var r in arr.EnumerateArray().Take(3))
        {
            var title = r.TryGetProperty("title", out var t) ? t.GetString() : "(no title)";
            var link = r.TryGetProperty("link", out var l) ? l.GetString() : "";
            Console.WriteLine($"  • {title}");
            if (!string.IsNullOrEmpty(link))
                Console.WriteLine($"    {link}");
        }
        Console.WriteLine();
    }
}
finally
{
    foreach (var (_, call) in tasks)
    {
        if (call.IsCompletedSuccessfully)
            call.Result.Dispose();
    }
}
