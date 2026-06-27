using SerpApi;

var apiKey = args.Length > 0 ? args[0] : Environment.GetEnvironmentVariable("SERPAPI_KEY");
if (string.IsNullOrEmpty(apiKey))
{
    Console.WriteLine("Usage: dotnet run -- <API_KEY>");
    return;
}

using var client = new SerpApiClient(apiKey);

Console.WriteLine("Fetching up to 3 pages of results...\n");

int pageNum = 0;
await foreach (var page in client.SearchPagesAsync(
    new Dictionary<string, string>
    {
        ["engine"] = "google",
        ["q"] = "dotnet async patterns",
        ["num"] = "5"
    },
    maxPages: 3))
{
    pageNum++;
    Console.WriteLine($"--- Page {pageNum} ---");

    if (page.OrganicResults != null)
    {
        foreach (var result in page.OrganicResults.Value.EnumerateArray())
        {
            var title = result.GetProperty("title").GetString();
            Console.WriteLine($"  {title}");
        }
    }

    page.Dispose();
    Console.WriteLine();
}

Console.WriteLine($"Total pages fetched: {pageNum}");
