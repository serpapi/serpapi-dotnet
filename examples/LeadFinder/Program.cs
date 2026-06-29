// Lead Finder: discover local businesses for outreach using Google Maps.
// Use case: Sales teams finding leads by location and category.
// Runs 1 API call, extracts business names, ratings, and addresses.

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

// Find plumbers in Austin, TX via Google Maps
Console.WriteLine("Finding leads: plumbers in Austin, TX\n");

using var results = await client.SearchAsync(new Dictionary<string, string>
{
    ["engine"] = "google_maps",
    ["q"] = "plumbers",
    ["ll"] = "@30.2672,-97.7431,14z",
    ["type"] = "search"
});

var localResults = results["local_results"];
if (localResults is not { } leads)
{
    Console.WriteLine("No results found.");
    return;
}

Console.WriteLine($"Found {leads.GetArrayLength()} leads:\n");

foreach (var lead in leads.EnumerateArray().Take(10))
{
    var name = lead.TryGetProperty("title", out var t) ? t.GetString() : "Unknown";
    var rating = lead.TryGetProperty("rating", out var r) ? r.ToString() : "N/A";
    var reviews = lead.TryGetProperty("reviews", out var rv) ? rv.ToString() : "0";
    var address = lead.TryGetProperty("address", out var a) ? a.GetString() : "";
    var phone = lead.TryGetProperty("phone", out var p) ? p.GetString() : "";

    Console.WriteLine($"  {name}");
    Console.WriteLine($"    Rating: {rating} ({reviews} reviews)");
    if (!string.IsNullOrEmpty(address)) Console.WriteLine($"    Address: {address}");
    if (!string.IsNullOrEmpty(phone)) Console.WriteLine($"    Phone: {phone}");
    Console.WriteLine();
}
