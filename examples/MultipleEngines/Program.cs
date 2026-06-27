using SerpApi;

var apiKey = args.Length > 0 ? args[0] : Environment.GetEnvironmentVariable("SERPAPI_KEY");
if (string.IsNullOrEmpty(apiKey))
{
    Console.WriteLine("Usage: dotnet run -- <API_KEY>");
    return;
}

using var client = new SerpApiClient(apiKey);

// Google
Console.WriteLine("=== Google Light ===");
using var google = await client.SearchAsync(new Dictionary<string, string>
{
    ["engine"] = "google_light",
    ["q"] = "coffee shops"
});
Console.WriteLine($"  Results: {google.OrganicResults?.GetArrayLength()}");

// Bing
Console.WriteLine("\n=== Bing ===");
using var bing = await client.SearchAsync(new Dictionary<string, string>
{
    ["engine"] = "bing",
    ["q"] = "coffee shops"
});
Console.WriteLine($"  Results: {bing.OrganicResults?.GetArrayLength()}");

// YouTube
Console.WriteLine("\n=== YouTube ===");
using var youtube = await client.SearchAsync(new Dictionary<string, string>
{
    ["engine"] = "youtube",
    ["search_query"] = "latte art"
});
var videos = youtube["video_results"];
if (videos != null)
{
    foreach (var video in videos.Value.EnumerateArray().Take(3))
    {
        Console.WriteLine($"  {video.GetProperty("title").GetString()}");
    }
}

// Google Maps
Console.WriteLine("\n=== Google Maps ===");
using var maps = await client.SearchAsync(new Dictionary<string, string>
{
    ["engine"] = "google_maps",
    ["q"] = "coffee",
    ["ll"] = "@30.267153,-97.7430608,14z",
    ["type"] = "search"
});
var places = maps["local_results"];
if (places != null)
{
    foreach (var place in places.Value.EnumerateArray().Take(3))
    {
        Console.WriteLine($"  {place.GetProperty("title").GetString()}");
    }
}

// Locations API (free, no credits)
Console.WriteLine("\n=== Locations ===");
var locations = await client.LocationAsync("Austin, TX", limit: 3);
foreach (var loc in locations.EnumerateArray())
{
    Console.WriteLine($"  {loc.GetProperty("name").GetString()}");
}
