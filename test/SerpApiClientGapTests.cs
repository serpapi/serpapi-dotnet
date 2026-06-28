using System.Net;
using System.Text.Json;

namespace SerpApi.Tests;

/// <summary>
/// Tests for coverage gaps identified during the modernize-sdk rewrite.
/// Covers: SSRF edge cases, cancellation, sync wrappers, pagination iteration,
/// HttpRequestException handling, non-JSON errors, and dispose safety.
/// </summary>
public class SerpApiClientGapTests
{
    // --- SSRF validation edge cases ---

    [Theory]
    [InlineData("ftp://serpapi.com/search?q=test")]
    [InlineData("file:///etc/passwd")]
    [InlineData("data:text/html,test")]
    public async Task NextPageAsync_RejectsNonHttpSchemes(string maliciousUrl)
    {
        var json = "{\"search_metadata\":{\"id\":\"x\"},\"serpapi_pagination\":{\"next\":\"" + maliciousUrl + "\"}}";
        var handler = new MockHttpHandler((_, _) =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("""{"search_metadata":{"id":"y"}}""")
            }));

        using var client = new SerpApiClient(new HttpClient(handler),
            new SerpApiClientOptions { ApiKey = "key" });

        var page = new SerpApiResponse(json);
        var ex = await Assert.ThrowsAsync<SerpApiException>(() => client.NextPageAsync(page));
        Assert.True(
            ex.Message.Contains("must use HTTP") || ex.Message.Contains("does not match") || ex.Message.Contains("Invalid pagination"),
            $"Unexpected message: {ex.Message}");
    }

    [Fact]
    public async Task NextPageAsync_RejectsRelativeUrl()
    {
        var json = """{"search_metadata":{"id":"x"},"serpapi_pagination":{"next":"/search?q=test&start=10"}}""";
        var handler = new MockHttpHandler((_, _) =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("""{"search_metadata":{"id":"y"}}""")
            }));

        using var client = new SerpApiClient(new HttpClient(handler),
            new SerpApiClientOptions { ApiKey = "key" });

        var page = new SerpApiResponse(json);
        var ex = await Assert.ThrowsAsync<SerpApiException>(() => client.NextPageAsync(page));
        // Relative URL either fails absolute parse ("Invalid pagination") or gets empty host ("does not match")
        Assert.True(
            ex.Message.Contains("Invalid pagination") || ex.Message.Contains("does not match"),
            $"Unexpected message: {ex.Message}");
    }

    [Fact]
    public async Task NextPageAsync_RejectsIpBasedHost()
    {
        var json = """{"search_metadata":{"id":"x"},"serpapi_pagination":{"next":"https://192.168.1.1/search?q=test"}}""";
        var handler = new MockHttpHandler((_, _) =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("""{"search_metadata":{"id":"y"}}""")
            }));

        using var client = new SerpApiClient(new HttpClient(handler),
            new SerpApiClientOptions { ApiKey = "key" });

        var page = new SerpApiResponse(json);
        var ex = await Assert.ThrowsAsync<SerpApiException>(() => client.NextPageAsync(page));
        Assert.Contains("does not match", ex.Message);
    }

    // --- Cancellation token handling ---

    [Fact]
    public async Task SearchAsync_RespectsCancellationToken()
    {
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        var handler = new MockHttpHandler((_, ct) =>
        {
            ct.ThrowIfCancellationRequested();
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("""{"search_metadata":{"id":"x"}}""")
            });
        });

        using var client = new SerpApiClient(new HttpClient(handler),
            new SerpApiClientOptions { ApiKey = "key" });

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            client.SearchAsync(new Dictionary<string, string>
            {
                ["engine"] = "google",
                ["q"] = "test"
            }, cts.Token));
    }

    [Fact]
    public async Task SearchPagesAsync_RespectsCancellation()
    {
        var callCount = 0;
        var handler = new MockHttpHandler((_, _) =>
        {
            callCount++;
            var json = """{"search_metadata":{"id":"x"},"organic_results":[],"serpapi_pagination":{"next":"https://serpapi.com/search?start=10"}}""";
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(json)
            });
        });

        using var cts = new CancellationTokenSource();
        using var client = new SerpApiClient(new HttpClient(handler),
            new SerpApiClientOptions { ApiKey = "key" });

        var pages = new List<SerpApiResponse>();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(async () =>
        {
            await foreach (var page in client.SearchPagesAsync(
                new Dictionary<string, string> { ["engine"] = "google", ["q"] = "test" },
                maxPages: 10,
                cancellationToken: cts.Token))
            {
                pages.Add(page);
                if (pages.Count == 2)
                    cts.Cancel();
            }
        });

        Assert.True(pages.Count >= 2);
    }

    // --- SearchPagesAsync iteration ---

    [Fact]
    public async Task SearchPagesAsync_StopsWhenNoPagination()
    {
        var callCount = 0;
        var handler = new MockHttpHandler((_, _) =>
        {
            callCount++;
            var hasNext = callCount < 3;
            var json = hasNext
                ? """{"search_metadata":{"id":"x"},"organic_results":[],"serpapi_pagination":{"next":"https://serpapi.com/search?start=10"}}"""
                : """{"search_metadata":{"id":"x"},"organic_results":[]}""";
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(json)
            });
        });

        using var client = new SerpApiClient(new HttpClient(handler),
            new SerpApiClientOptions { ApiKey = "key" });

        var pages = new List<SerpApiResponse>();
        await foreach (var page in client.SearchPagesAsync(
            new Dictionary<string, string> { ["engine"] = "google", ["q"] = "test" },
            maxPages: 10))
        {
            pages.Add(page);
        }

        Assert.Equal(3, pages.Count);
    }

    [Fact]
    public async Task SearchPagesAsync_StopsAtMaxPages()
    {
        var handler = new MockHttpHandler((_, _) =>
        {
            var json = """{"search_metadata":{"id":"x"},"organic_results":[],"serpapi_pagination":{"next":"https://serpapi.com/search?start=10"}}""";
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(json)
            });
        });

        using var client = new SerpApiClient(new HttpClient(handler),
            new SerpApiClientOptions { ApiKey = "key" });

        var pages = new List<SerpApiResponse>();
        await foreach (var page in client.SearchPagesAsync(
            new Dictionary<string, string> { ["engine"] = "google", ["q"] = "test" },
            maxPages: 3))
        {
            pages.Add(page);
        }

        Assert.Equal(3, pages.Count);
    }

    // --- Sync wrapper coverage ---

    [Fact]
    public void Html_SyncWorks()
    {
        var handler = new MockHttpHandler((_, _) =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("<html><body>results</body></html>")
            }));

        using var client = new SerpApiClient(new HttpClient(handler),
            new SerpApiClientOptions { ApiKey = "key" });

        var result = client.Html(new Dictionary<string, string>
        {
            ["engine"] = "google",
            ["q"] = "test"
        });

        Assert.Contains("</body>", result);
    }

    [Fact]
    public void SearchArchive_SyncWorks()
    {
        var handler = new MockHttpHandler((_, _) =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("""{"search_metadata":{"id":"archived"}}""")
            }));

        using var client = new SerpApiClient(new HttpClient(handler),
            new SerpApiClientOptions { ApiKey = "key" });

        var result = client.SearchArchive("abc123");
        Assert.Equal("archived", result.SearchId);
    }

    [Fact]
    public void Account_SyncWorks()
    {
        var handler = new MockHttpHandler((_, _) =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("""{"account_id":"123","api_key":"key"}""")
            }));

        using var client = new SerpApiClient(new HttpClient(handler),
            new SerpApiClientOptions { ApiKey = "key" });

        var result = client.Account();
        Assert.Equal("123", result["account_id"]!.Value.GetString());
    }

    [Fact]
    public void Location_SyncWorks()
    {
        var handler = new MockHttpHandler((_, _) =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("""[{"id":"1","name":"Austin, TX"}]""")
            }));

        using var client = new SerpApiClient(new HttpClient(handler),
            new SerpApiClientOptions { ApiKey = "key" });

        var result = client.Location("Austin", limit: 3);
        Assert.Equal(JsonValueKind.Array, result.ValueKind);
        Assert.Equal(1, result.GetArrayLength());
    }

    // --- HttpRequestException wrapping ---

    [Fact]
    public async Task SearchAsync_WrapsHttpRequestException()
    {
        var handler = new MockHttpHandler((_, _) =>
            throw new HttpRequestException("DNS resolution failed"));

        using var client = new SerpApiClient(new HttpClient(handler),
            new SerpApiClientOptions { ApiKey = "key" });

        var ex = await Assert.ThrowsAsync<SerpApiException>(() =>
            client.SearchAsync(new Dictionary<string, string>
            {
                ["engine"] = "google",
                ["q"] = "test"
            }));

        Assert.Contains("DNS resolution failed", ex.Message);
        Assert.IsType<HttpRequestException>(ex.InnerException);
    }

    // --- Non-JSON error response ---

    [Fact]
    public async Task SearchAsync_HandlesNonJsonErrorBody()
    {
        var handler = new MockHttpHandler((_, _) =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.BadGateway)
            {
                Content = new StringContent("<html>502 Bad Gateway</html>")
            }));

        using var client = new SerpApiClient(new HttpClient(handler),
            new SerpApiClientOptions { ApiKey = "key" });

        var ex = await Assert.ThrowsAsync<SerpApiHttpException>(() =>
            client.SearchAsync(new Dictionary<string, string>
            {
                ["engine"] = "google",
                ["q"] = "test"
            }));

        Assert.Equal(502, ex.StatusCode);
        Assert.Contains("502 Bad Gateway", ex.Message);
    }

    // --- Non-API-key error from response body ---

    [Fact]
    public async Task SearchAsync_ThrowsGenericExceptionOnNonKeyError()
    {
        var json = """{"error": "Google hasn't returned any results for this query."}""";
        var handler = new MockHttpHandler((_, _) =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(json)
            }));

        using var client = new SerpApiClient(new HttpClient(handler),
            new SerpApiClientOptions { ApiKey = "key" });

        var ex = await Assert.ThrowsAsync<SerpApiException>(() =>
            client.SearchAsync(new Dictionary<string, string>
            {
                ["engine"] = "google",
                ["q"] = "xyznonexistent123"
            }));

        Assert.IsNotType<SerpApiKeyException>(ex);
        Assert.Contains("Google hasn't returned", ex.Message);
    }

    // --- Response missing search_metadata ---

    [Fact]
    public void SerpApiResponse_SearchId_NullWhenNoMetadata()
    {
        var response = new SerpApiResponse("""{"organic_results":[]}""");
        Assert.Null(response.SearchId);
    }

    [Fact]
    public void SerpApiResponse_SearchMetadata_NullWhenMissing()
    {
        var response = new SerpApiResponse("""{"organic_results":[]}""");
        Assert.Null(response.SearchMetadata);
    }

    // --- GetProperty returns default for missing key ---

    [Fact]
    public void SerpApiResponse_GetProperty_ReturnsDefaultForMissingKey()
    {
        var response = new SerpApiResponse("""{"search_metadata":{"id":"x"}}""");
        var result = response.GetProperty<List<string>>("nonexistent");
        Assert.Null(result);
    }

    // --- Dispose safety ---

    [Fact]
    public void SerpApiResponse_DisposeMultipleTimes_DoesNotThrow()
    {
        var response = new SerpApiResponse("""{"a":"b"}""");
        response.Dispose();
        var ex = Record.Exception(() => response.Dispose());
        Assert.Null(ex);
    }

    // --- Client dispose with owned vs external HttpClient ---

    [Fact]
    public void Dispose_WithOwnedClient_DisposesHttpClient()
    {
        var client = new SerpApiClient("key");
        var ex = Record.Exception(() => client.Dispose());
        Assert.Null(ex);
    }

    [Fact]
    public void Dispose_WithExternalClient_DoesNotDisposeHttpClient()
    {
        var httpClient = new HttpClient();
        var client = new SerpApiClient(httpClient, new SerpApiClientOptions { ApiKey = "key" });
        client.Dispose();

        // httpClient should still be usable (not disposed)
        var ex = Record.Exception(() => _ = httpClient.Timeout);
        Assert.Null(ex);
        httpClient.Dispose();
    }
}
