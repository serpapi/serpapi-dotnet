using Microsoft.Extensions.DependencyInjection;
using SerpApi;

var apiKey = args.Length > 0 ? args[0] : Environment.GetEnvironmentVariable("SERPAPI_KEY");
if (string.IsNullOrEmpty(apiKey))
{
    Console.WriteLine("Usage: dotnet run -- <API_KEY>");
    return;
}

// Configure services (like ASP.NET Core's Program.cs)
var services = new ServiceCollection();

services.AddSerpApi(options =>
{
    options.ApiKey = apiKey;
    options.Timeout = TimeSpan.FromSeconds(30);
});

// Simulates what ASP.NET Core does at startup
var provider = services.BuildServiceProvider();

// Resolve from DI (like constructor injection in a controller)
var client = provider.GetRequiredService<SerpApiClient>();

using var results = await client.SearchAsync(new Dictionary<string, string>
{
    ["engine"] = "google",
    ["q"] = "dependency injection patterns"
});

Console.WriteLine($"Search ID: {results.SearchId}");
Console.WriteLine($"Results: {results.OrganicResults?.GetArrayLength()}");

// The client lifecycle is managed by DI — no manual dispose needed
