// Price Monitor: track product prices across shopping engines.
// Use case: E-commerce teams monitoring competitor pricing in real time.
// Runs concurrent searches on Google Shopping and Walmart.

using SerpApi;
using System.Text.Json;

var apiKey = args.Length > 0 ? args[0] : Environment.GetEnvironmentVariable("SERPAPI_KEY");
if (string.IsNullOrEmpty(apiKey))
{
    Console.WriteLine("Usage: dotnet run -- <API_KEY>");
    Console.WriteLine("   or: SERPAPI_KEY=... dotnet run");
    return;
}

using var client = new SerpApiClient(apiKey, new SerpApiClientOptions
{
    Timeout = TimeSpan.FromSeconds(30)
});

var product = "sony wh-1000xm5";
Console.WriteLine($"Monitoring prices for: \"{product}\"\n");

using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(20));

// Search Google Shopping and Walmart concurrently
var googleShopping = client.SearchAsync(new Dictionary<string, string>
{
    ["engine"] = "google_shopping",
    ["q"] = product
}, cts.Token);

var walmart = client.SearchAsync(new Dictionary<string, string>
{
    ["engine"] = "walmart",
    ["query"] = product
}, cts.Token);

try
{
    await Task.WhenAll(googleShopping, walmart);

    // Google Shopping results
    Console.WriteLine("=== Google Shopping ===");
    var gResults = await googleShopping;
    var shopping = gResults["shopping_results"];
    if (shopping is { } items)
    {
        foreach (var item in items.EnumerateArray().Take(5))
        {
            var title = item.TryGetProperty("title", out var t) ? t.GetString() : "?";
            var price = item.TryGetProperty("extracted_price", out var p) ? $"${p}" : "N/A";
            var source = item.TryGetProperty("source", out var s) ? s.GetString() : "";
            Console.WriteLine($"  {price,-8} {source,-15} {title}");
        }
    }
    else Console.WriteLine("  No results");

    // Walmart results
    Console.WriteLine("\n=== Walmart ===");
    var wResults = await walmart;
    var organic = wResults["organic_results"];
    if (organic is { } walmartItems)
    {
        foreach (var item in walmartItems.EnumerateArray().Take(5))
        {
            var title = item.TryGetProperty("title", out var t) ? t.GetString() : "?";
            var price = item.TryGetProperty("primary_offer", out var po)
                && po.TryGetProperty("offer_price", out var op)
                    ? $"${op}"
                    : "N/A";
            Console.WriteLine($"  {price,-8} {title}");
        }
    }
    else Console.WriteLine("  No results");
}
catch (OperationCanceledException) when (cts.IsCancellationRequested)
{
    Console.WriteLine("Price search timed out after 20 seconds.");
}
catch (SerpApiException ex)
{
    Console.WriteLine($"Search error: {ex.Message}");
}
finally
{
    DisposeCompleted(googleShopping);
    DisposeCompleted(walmart);
}

static void DisposeCompleted(Task<SerpApiResponse> task)
{
    if (task.Status == TaskStatus.RanToCompletion)
        task.Result.Dispose();
}
