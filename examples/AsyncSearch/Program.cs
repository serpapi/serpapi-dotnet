using SerpApi;

var apiKey = args.Length > 0 ? args[0] : Environment.GetEnvironmentVariable("SERPAPI_KEY");
if (string.IsNullOrEmpty(apiKey))
{
    Console.WriteLine("Usage: dotnet run -- <API_KEY>");
    return;
}

using var client = new SerpApiClient(apiKey, new SerpApiClientOptions
{
    Timeout = TimeSpan.FromSeconds(30)
});

using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(20));

// Run multiple searches concurrently
var tasks = new[]
{
    client.SearchAsync(new Dictionary<string, string>
    {
        ["engine"] = "google_light",
        ["q"] = "async programming"
    }, cts.Token),
    client.SearchAsync(new Dictionary<string, string>
    {
        ["engine"] = "google_light",
        ["q"] = "parallel computing"
    }, cts.Token)
};

var results = await Task.WhenAll(tasks);

foreach (var result in results)
{
    var query = result.SearchParameters!.Value.GetProperty("q").GetString();
    var count = result.OrganicResults?.GetArrayLength() ?? 0;
    Console.WriteLine($"'{query}' → {count} results");
    result.Dispose();
}

// Account info (free, no credits)
using var account = await client.AccountAsync(cts.Token);
Console.WriteLine($"\nPlan: {account["plan_id"]}");
