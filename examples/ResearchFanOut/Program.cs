using SerpApi;
using System.Text.Json;

var apiKey = args.Length > 0 ? args[0] : Environment.GetEnvironmentVariable("SERPAPI_KEY");
if (string.IsNullOrEmpty(apiKey))
{
    Console.WriteLine("Usage: dotnet run -- <API_KEY>");
    return;
}

using var client = new SerpApiClient(apiKey);
var topic = "Apple AAPL stock";

// === Research Fan-Out ===
// Query multiple engines in parallel — the core pattern for AI agent research.
// One user question → multiple surfaces queried concurrently.
Console.WriteLine($"Researching: {topic}\n");

var tasks = new[]
{
    client.SearchAsync(new Dictionary<string, string>
    {
        ["engine"] = "google_light",
        ["q"] = "AAPL analyst consensus 2026",
        ["num"] = "10"
    }),
    client.SearchAsync(new Dictionary<string, string>
    {
        ["engine"] = "google_news_light",
        ["q"] = "Apple earnings revenue"
    }),
    client.SearchAsync(new Dictionary<string, string>
    {
        ["engine"] = "google_finance",
        ["q"] = "AAPL:NASDAQ"
    })
};

var results = await Task.WhenAll(tasks);

Console.WriteLine("=== Web Results (google_light) ===");
if (results[0].OrganicResults is { } web)
{
    foreach (var r in web.EnumerateArray().Take(3))
        Console.WriteLine($"  • {r.GetProperty("title").GetString()}");
}

Console.WriteLine("\n=== News (google_news_light) ===");
var news = results[1]["news_results"];
if (news is { } newsArr)
{
    foreach (var r in newsArr.EnumerateArray().Take(3))
        Console.WriteLine($"  • {r.GetProperty("title").GetString()}");
}

Console.WriteLine("\n=== Finance (google_finance) ===");
var summary = results[2]["summary"];
if (summary is { } s)
{
    var props = new[] { "price", "currency", "previous_close" };
    foreach (var prop in props)
    {
        if (s.TryGetProperty(prop, out var val))
            Console.WriteLine($"  {prop}: {val}");
    }
}

foreach (var r in results) r.Dispose();

// === Progressive Refinement ===
// Start narrow, broaden on empty results.
Console.WriteLine("\n=== Progressive Refinement ===");

using var narrow = await client.SearchAsync(new Dictionary<string, string>
{
    ["engine"] = "google_light",
    ["q"] = "\"AAPL Q2 2026 earnings beat\"",
    ["num"] = "5"
});

var narrowCount = narrow.OrganicResults?.GetArrayLength() ?? 0;
Console.WriteLine($"  Exact phrase: {narrowCount} results");

if (narrowCount == 0)
{
    Console.WriteLine("  → Broadening query...");
    using var broad = await client.SearchAsync(new Dictionary<string, string>
    {
        ["engine"] = "google_light",
        ["q"] = "AAPL Q2 2026 earnings",
        ["num"] = "10"
    });
    Console.WriteLine($"  Broad query: {broad.OrganicResults?.GetArrayLength() ?? 0} results");
}

// === Verification Loop ===
// Cross-reference a claim across independent engines.
Console.WriteLine("\n=== Verification Loop ===");
var claim = "Apple revenue exceeded $100 billion";

var verifyTasks = new[]
{
    client.SearchAsync(new Dictionary<string, string>
    {
        ["engine"] = "google_light",
        ["q"] = claim,
        ["num"] = "3"
    }),
    client.SearchAsync(new Dictionary<string, string>
    {
        ["engine"] = "bing",
        ["q"] = claim,
        ["count"] = "3"
    })
};

var verifyResults = await Task.WhenAll(verifyTasks);

var googleHits = verifyResults[0].OrganicResults?.GetArrayLength() ?? 0;
var bingHits = verifyResults[1].OrganicResults?.GetArrayLength() ?? 0;

Console.WriteLine($"  Google: {googleHits} results");
Console.WriteLine($"  Bing:   {bingHits} results");
Console.WriteLine($"  Confidence: {(googleHits > 0 && bingHits > 0 ? "HIGH" : "LOW — needs manual review")}");

foreach (var r in verifyResults) r.Dispose();
