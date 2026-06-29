// AI Research Agent: gather multi-source context for LLM/RAG pipelines.
// Use case: Feed real-time search data into an AI assistant or RAG system.
// Fans out across web, news, and scholar — returns structured context.

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

var topic = args.Length > 1 ? args[1] : "retrieval augmented generation";
Console.WriteLine($"Research topic: \"{topic}\"\n");

// Fan-out: web + news + academic papers in parallel (RAG context gathering)
var web = client.SearchAsync(new Dictionary<string, string>
{
    ["engine"] = "google_light",
    ["q"] = topic,
    ["num"] = "5"
});

var news = client.SearchAsync(new Dictionary<string, string>
{
    ["engine"] = "google_news_light",
    ["q"] = topic
});

var scholar = client.SearchAsync(new Dictionary<string, string>
{
    ["engine"] = "google_scholar",
    ["q"] = topic,
    ["as_ylo"] = DateTime.UtcNow.Year.ToString()
});

await Task.WhenAll(web, news, scholar);

var sources = new List<(string Type, string Title, string Url, string Snippet)>();

try
{
    // Collect web results
    using var webResult = web.Result;
    if (webResult.OrganicResults is { } webItems)
    {
        foreach (var r in webItems.EnumerateArray().Take(5))
        {
            sources.Add((
                "web",
                r.TryGetProperty("title", out var t) ? t.GetString() ?? "" : "",
                r.TryGetProperty("link", out var l) ? l.GetString() ?? "" : "",
                r.TryGetProperty("snippet", out var s) ? s.GetString() ?? "" : ""
            ));
        }
    }

    // Collect news
    using var newsResult = news.Result;
    var newsItems = newsResult["news_results"];
    if (newsItems is { } ni)
    {
        foreach (var r in ni.EnumerateArray().Take(3))
        {
            sources.Add((
                "news",
                r.TryGetProperty("title", out var t) ? t.GetString() ?? "" : "",
                r.TryGetProperty("link", out var l) ? l.GetString() ?? "" : "",
                r.TryGetProperty("snippet", out var s) ? s.GetString() ?? "" : ""
            ));
        }
    }

    // Collect academic papers
    using var scholarResult = scholar.Result;
    if (scholarResult.OrganicResults is { } papers)
    {
        foreach (var r in papers.EnumerateArray().Take(3))
        {
            sources.Add((
                "scholar",
                r.TryGetProperty("title", out var t) ? t.GetString() ?? "" : "",
                r.TryGetProperty("link", out var l) ? l.GetString() ?? "" : "",
                r.TryGetProperty("snippet", out var s) ? s.GetString() ?? "" : ""
            ));
        }
    }

    // Output structured context (ready for LLM prompt injection)
    Console.WriteLine($"Gathered {sources.Count} sources for RAG context:\n");

    foreach (var group in sources.GroupBy(s => s.Type))
    {
        Console.WriteLine($"=== {group.Key.ToUpper()} ===");
        foreach (var (type, title, url, snippet) in group)
        {
            Console.WriteLine($"  • {title}");
            if (!string.IsNullOrEmpty(snippet))
                Console.WriteLine($"    {snippet[..Math.Min(100, snippet.Length)]}...");
            Console.WriteLine($"    {url}\n");
        }
    }

    // In a real RAG pipeline, you'd format these as context and pass to an LLM:
    // var prompt = $"Based on these sources:\n{FormatSources(sources)}\n\nAnswer: {topic}";
}
catch (SerpApiException ex)
{
    Console.WriteLine($"Search error: {ex.Message}");
}
