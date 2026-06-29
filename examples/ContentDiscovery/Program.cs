// Content Discovery: find trending topics and content gaps for a niche.
// Use case: Content marketers finding what to write about next.
// Progressive refinement: trending news → related questions → competitor content.

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

var niche = "sustainable fashion";
Console.WriteLine($"Content discovery for: \"{niche}\"\n");

// Step 1: What's trending in news?
Console.WriteLine("=== Trending News ===");
using var newsResults = await client.SearchAsync(new Dictionary<string, string>
{
    ["engine"] = "google_news_light",
    ["q"] = niche
});

var newsItems = newsResults["news_results"];
if (newsItems is { } news)
{
    foreach (var item in news.EnumerateArray().Take(5))
    {
        var title = item.TryGetProperty("title", out var t) ? t.GetString() : "";
        var date = item.TryGetProperty("date", out var d) ? d.GetString() : "";
        Console.WriteLine($"  • {title} ({date})");
    }
}

// Step 2: What questions do people ask?
Console.WriteLine("\n=== People Also Ask ===");
using var searchResults = await client.SearchAsync(new Dictionary<string, string>
{
    ["engine"] = "google_light",
    ["q"] = niche
});

var paa = searchResults["related_questions"];
if (paa is { } questions)
{
    foreach (var q in questions.EnumerateArray().Take(5))
    {
        var question = q.TryGetProperty("question", out var qText) ? qText.GetString() : "";
        Console.WriteLine($"  • {question}");
    }
}

// Step 3: What are competitors ranking for?
Console.WriteLine("\n=== Top Organic Competitors ===");
if (searchResults.OrganicResults is { } organic)
{
    var domains = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
    foreach (var r in organic.EnumerateArray())
    {
        var link = r.TryGetProperty("link", out var l) ? l.GetString() ?? "" : "";
        if (Uri.TryCreate(link, UriKind.Absolute, out var uri))
        {
            if (domains.Add(uri.Host))
            {
                var title = r.TryGetProperty("title", out var t) ? t.GetString() : "";
                Console.WriteLine($"  • {uri.Host,-30} \"{title}\"");
            }
        }
        if (domains.Count >= 5) break;
    }
}

// Step 4: Related searches (content gap opportunities)
Console.WriteLine("\n=== Content Gap Ideas (Related Searches) ===");
var related = searchResults["related_searches"];
if (related is { } relatedItems)
{
    foreach (var r in relatedItems.EnumerateArray().Take(8))
    {
        var query = r.TryGetProperty("query", out var q) ? q.GetString() : "";
        Console.WriteLine($"  • {query}");
    }
}
