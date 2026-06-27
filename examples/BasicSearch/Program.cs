using SerpApi;

var apiKey = args.Length > 0 ? args[0] : Environment.GetEnvironmentVariable("SERPAPI_KEY");
if (string.IsNullOrEmpty(apiKey))
{
    Console.WriteLine("Usage: dotnet run -- <API_KEY>");
    return;
}

using var client = new SerpApiClient(apiKey);

using var results = client.Search(new Dictionary<string, string>
{
    ["engine"] = "google_light",
    ["q"] = "coffee",
    ["location"] = "Austin, Texas",
    ["num"] = "5"
});

Console.WriteLine($"Search ID: {results.SearchId}");

foreach (var result in results.OrganicResults!.Value.EnumerateArray())
{
    var pos = result.GetProperty("position");
    var title = result.GetProperty("title").GetString();
    Console.WriteLine($"  {pos}. {title}");
}
