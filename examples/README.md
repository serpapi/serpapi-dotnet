# Examples

Self-contained examples for the SerpApi .NET SDK. Each folder is a standalone console app.

## Running

```bash
cd examples/BasicSearch
dotnet run -- YOUR_API_KEY
```

Or set the `SERPAPI_KEY` environment variable:

```bash
export SERPAPI_KEY=your_key_here
cd examples/BasicSearch
dotnet run
```

## Examples

| Example | Description |
|---------|-------------|
| [BasicSearch](BasicSearch/) | Minimal synchronous search |
| [AsyncSearch](AsyncSearch/) | Async/await with CancellationToken |
| [Pagination](Pagination/) | Iterate pages with IAsyncEnumerable |
| [MultipleEngines](MultipleEngines/) | Google, Bing, YouTube, Google Maps |
| [ErrorHandling](ErrorHandling/) | Exception types and retry pattern |
| [DependencyInjection](DependencyInjection/) | ASP.NET Core / generic host setup |
| [ResearchFanOut](ResearchFanOut/) | Multi-engine parallel research, progressive refinement, verification loop |
