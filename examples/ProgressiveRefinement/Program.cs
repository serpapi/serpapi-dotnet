// Progressive Refinement: start with an exact phrase, broaden until you get results.
// Demonstrates: sequential refinement strategy, time-filtered search.
// Runs 1–3 API calls depending on result availability.

using SerpApi;

var apiKey = args.Length > 0 ? args[0] : Environment.GetEnvironmentVariable("SERPAPI_KEY");
if (string.IsNullOrEmpty(apiKey))
{
    Console.WriteLine("Usage: dotnet run -- <API_KEY>");
    Console.WriteLine("   or: SERPAPI_KEY=... dotnet run");
    return;
}

using var client = new SerpApiClient(apiKey);

// Step 1: exact phrase — high precision, may return 0 results.
Console.WriteLine("=== Step 1: Exact phrase ===");
using var exact = await client.SearchAsync(new Dictionary<string, string>
{
    ["engine"] = "google_light",
    ["q"] = "\"best pour over coffee ratio\""
});

var exactCount = exact.OrganicResults?.GetArrayLength() ?? 0;
Console.WriteLine($"  Results: {exactCount}");

if (exactCount >= 3)
{
    PrintTop(exact, 3);
    return;
}

// Step 2: drop quotes — broader match.
Console.WriteLine("\n=== Step 2: Broad keywords ===");
using var broad = await client.SearchAsync(new Dictionary<string, string>
{
    ["engine"] = "google_light",
    ["q"] = "best pour over coffee ratio"
});

var broadCount = broad.OrganicResults?.GetArrayLength() ?? 0;
Console.WriteLine($"  Results: {broadCount}");

if (broadCount >= 3)
{
    PrintTop(broad, 5);
    return;
}

// Step 3: time-filtered — last month only.
Console.WriteLine("\n=== Step 3: Recent (past month) ===");
using var recent = await client.SearchAsync(new Dictionary<string, string>
{
    ["engine"] = "google_light",
    ["q"] = "pour over coffee ratio",
    ["tbs"] = "qdr:m"
});

var recentCount = recent.OrganicResults?.GetArrayLength() ?? 0;
Console.WriteLine($"  Results: {recentCount}");
PrintTop(recent, 5);

static void PrintTop(SerpApiResponse response, int max)
{
    if (response.OrganicResults is not { } results) return;
    foreach (var r in results.EnumerateArray().Take(max))
    {
        var title = r.TryGetProperty("title", out var t) ? t.GetString() : "(no title)";
        var link = r.TryGetProperty("link", out var l) ? l.GetString() : "";
        Console.WriteLine($"  • {title}");
        if (!string.IsNullOrEmpty(link))
            Console.WriteLine($"    {link}");
    }
}
