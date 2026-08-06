# Examples

Real-world use case examples for the SerpApi .NET SDK.

## Use Cases

| Example | Use Case | Engines Used | API Calls |
|---------|----------|-------------|-----------|
| [LeadFinder](LeadFinder/) | Lead generation | Google Maps | 1 |
| [CompetitorTracker](CompetitorTracker/) | SEO & competitive intel | Google Light, Bing | 2 (parallel) |
| [RankTracker](RankTracker/) | SEO rank monitoring | Google Light | 1–3 (pagination) |
| [PriceMonitor](PriceMonitor/) | Price monitoring | Google Shopping, Walmart | 2 (parallel) |
| [AiResearchAgent](AiResearchAgent/) | AI/RAG context gathering | Google Light, Google News, Google Scholar | 3 (parallel) |
| [ContentDiscovery](ContentDiscovery/) | Market research & content gaps | Google News, Google Light | 2 (sequential) |
| [ErrorHandling](ErrorHandling/) | Reliability patterns | — | 0–2 |
| [DependencyInjection](DependencyInjection/) | Enterprise DI integration | Google | 1 |

## Running

```bash
export SERPAPI_KEY=your_key_here
cd LeadFinder
dotnet run
```

Or pass the key directly:

```bash
dotnet run -- your_key_here
```
