using SerpApi;

// Demonstrate all exception types

// 1. Missing API key
Console.WriteLine("=== SerpApiKeyException (empty key) ===");
try
{
    var bad = new SerpApiClient("");
}
catch (SerpApiKeyException ex)
{
    Console.WriteLine($"  Caught: {ex.Message}");
}

// 2. Invalid API key (requires network)
var apiKey = args.Length > 0 ? args[0] : Environment.GetEnvironmentVariable("SERPAPI_KEY");
if (string.IsNullOrEmpty(apiKey))
{
    Console.WriteLine("\nSkipping network tests (no API key).");
    Console.WriteLine("Usage: dotnet run -- <API_KEY>");
    return;
}

Console.WriteLine("\n=== SerpApiKeyException (invalid key) ===");
using (var client = new SerpApiClient("invalid_key_12345"))
{
    try
    {
        await client.SearchAsync(new Dictionary<string, string>
        {
            ["engine"] = "google",
            ["q"] = "test"
        });
    }
    catch (SerpApiHttpException ex)
    {
        Console.WriteLine($"  HTTP {ex.StatusCode}: {ex.Message}");
    }
    catch (SerpApiKeyException ex)
    {
        Console.WriteLine($"  Caught: {ex.Message}");
    }
}

// 3. Timeout
Console.WriteLine("\n=== SerpApiTimeoutException ===");
using (var client = new SerpApiClient(apiKey, new SerpApiClientOptions
{
    Timeout = TimeSpan.FromMilliseconds(1) // impossibly short
}))
{
    try
    {
        await client.SearchAsync(new Dictionary<string, string>
        {
            ["engine"] = "google",
            ["q"] = "timeout test"
        });
    }
    catch (SerpApiTimeoutException ex)
    {
        Console.WriteLine($"  Caught: {ex.Message}");
    }
}

// 4. Successful request with error handling
Console.WriteLine("\n=== Successful search with error handling ===");
using var safeClient = new SerpApiClient(apiKey);
try
{
    using var results = await safeClient.SearchAsync(new Dictionary<string, string>
    {
        ["engine"] = "google",
        ["q"] = "error handling best practices"
    });
    Console.WriteLine($"  Success! Got {results.OrganicResults?.GetArrayLength()} results");
}
catch (SerpApiException ex)
{
    Console.WriteLine($"  Error: {ex.Message}");
}
