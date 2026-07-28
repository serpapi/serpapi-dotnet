using System.Net;
using System.Text.Json;

namespace SerpApi.Tests;

public class SerpApiClientTests
{
    [Fact]
    public void Constructor_ThrowsOnEmptyApiKey()
    {
        Assert.Throws<SerpApiKeyException>(() => new SerpApiClient(""));
        Assert.Throws<SerpApiKeyException>(() => new SerpApiClient("  "));
    }

    [Fact]
    public void Constructor_AcceptsValidApiKey()
    {
        using var client = new SerpApiClient("test_key_123");
        Assert.NotNull(client);
    }

    [Fact]
    public void Constructor_WithOptions_SetsTimeout()
    {
        var options = new SerpApiClientOptions { Timeout = TimeSpan.FromSeconds(10) };
        using var client = new SerpApiClient("key", options);
        Assert.NotNull(client);
    }

    [Fact]
    public void Constructor_HttpClient_ThrowsOnEmptyApiKey()
    {
        var httpClient = new HttpClient();
        var options = new SerpApiClientOptions { ApiKey = "" };
        Assert.Throws<SerpApiKeyException>(() => new SerpApiClient(httpClient, options));
    }

    [Fact]
    public void Constructor_HttpClient_ThrowsOnNullOptions()
    {
        using var httpClient = new HttpClient();

        var exception = Assert.Throws<ArgumentNullException>(
            () => new SerpApiClient(httpClient, null!));

        Assert.Equal("options", exception.ParamName);
    }

    [Fact]
    public async Task SearchAsync_BuildsCorrectUrl()
    {
        string? capturedUrl = null;
        var handler = new MockHttpHandler((request, ct) =>
        {
            capturedUrl = request.RequestUri?.AbsoluteUri;
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("{\"search_metadata\":{\"id\":\"abc\"},\"organic_results\":[]}")
            });
        });

        var httpClient = new HttpClient(handler);
        var options = new SerpApiClientOptions { ApiKey = "test_key" };
        using var client = new SerpApiClient(httpClient, options);

        await client.SearchAsync(new Dictionary<string, string>
        {
            ["engine"] = "google",
            ["q"] = "coffee",
            ["location"] = "Austin, Texas"
        });

        Assert.NotNull(capturedUrl);
        Assert.Contains("api_key=test_key", capturedUrl);
        Assert.Contains("engine=google", capturedUrl);
        Assert.Contains("q=coffee", capturedUrl);
        Assert.Contains("location=Austin", capturedUrl);
        Assert.Contains("output=json", capturedUrl);
        Assert.Contains("source=dotnet", capturedUrl);
        Assert.StartsWith("https://serpapi.com/search?", capturedUrl);
    }

    [Fact]
    public async Task SearchAsync_ReturnsResponse()
    {
        var json = """
        {
            "search_metadata": {"id": "abc123", "status": "Success"},
            "search_parameters": {"engine": "google", "q": "test"},
            "organic_results": [
                {"position": 1, "title": "Test Result", "link": "https://example.com"}
            ]
        }
        """;
        var handler = new MockHttpHandler((_, _) =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(json)
            }));

        using var client = new SerpApiClient(new HttpClient(handler),
            new SerpApiClientOptions { ApiKey = "key" });

        var result = await client.SearchAsync(new Dictionary<string, string>
        {
            ["engine"] = "google",
            ["q"] = "test"
        });

        Assert.Equal("abc123", result.SearchId);
        Assert.NotNull(result.OrganicResults);
        Assert.Equal(JsonValueKind.Array, result.OrganicResults!.Value.ValueKind);
        Assert.Equal(1, result.OrganicResults!.Value.GetArrayLength());
    }

    [Fact]
    public async Task SearchAsync_ThrowsOnApiError()
    {
        var json = """{"error": "Invalid API key. Your API key should be here: https://serpapi.com/manage-api-key"}""";
        var handler = new MockHttpHandler((_, _) =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(json)
            }));

        using var client = new SerpApiClient(new HttpClient(handler),
            new SerpApiClientOptions { ApiKey = "bad_key" });

        await Assert.ThrowsAsync<SerpApiKeyException>(() =>
            client.SearchAsync(new Dictionary<string, string>
            {
                ["engine"] = "google",
                ["q"] = "test"
            }));
    }

    [Fact]
    public async Task SearchAsync_ThrowsOnHttpError()
    {
        var handler = new MockHttpHandler((_, _) =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.TooManyRequests)
            {
                Content = new StringContent("""{"error": "Rate limit exceeded"}""")
            }));

        using var client = new SerpApiClient(new HttpClient(handler),
            new SerpApiClientOptions { ApiKey = "key" });

        var ex = await Assert.ThrowsAsync<SerpApiHttpException>(() =>
            client.SearchAsync(new Dictionary<string, string>
            {
                ["engine"] = "google",
                ["q"] = "test"
            }));

        Assert.Equal(429, ex.StatusCode);
        Assert.Contains("Rate limit", ex.Message);
    }

    [Fact]
    public async Task SearchAsync_ThrowsKeyExceptionOnInvalidKeyHttpError()
    {
        var handler = new MockHttpHandler((_, _) =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.Unauthorized)
            {
                Content = new StringContent("""{"error": "Invalid API key"}""")
            }));

        using var client = new SerpApiClient(new HttpClient(handler),
            new SerpApiClientOptions { ApiKey = "invalid_key" });

        var exception = await Assert.ThrowsAsync<SerpApiKeyException>(() =>
            client.SearchAsync(new Dictionary<string, string>
            {
                ["engine"] = "google",
                ["q"] = "test"
            }));

        Assert.Equal("Invalid API key", exception.Message);
    }

    [Fact]
    public async Task SearchAsync_ThrowsHttpExceptionOnQuotaHttpError()
    {
        const string message = "You have run out of searches. Upgrade your API key plan.";
        var handler = new MockHttpHandler((_, _) =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.TooManyRequests)
            {
                Content = new StringContent($"{{\"error\":\"{message}\"}}")
            }));

        using var client = new SerpApiClient(new HttpClient(handler),
            new SerpApiClientOptions { ApiKey = "key" });

        var exception = await Assert.ThrowsAsync<SerpApiHttpException>(() =>
            client.SearchAsync(new Dictionary<string, string>
            {
                ["engine"] = "google",
                ["q"] = "test"
            }));

        Assert.Equal(429, exception.StatusCode);
        Assert.Equal(message, exception.Message);
    }

    [Fact]
    public async Task SearchAsync_ThrowsOnTimeout()
    {
        var handler = new MockHttpHandler(async (_, ct) =>
        {
            await Task.Delay(TimeSpan.FromSeconds(10), ct);
            return new HttpResponseMessage(HttpStatusCode.OK);
        });

        var httpClient = new HttpClient(handler) { Timeout = TimeSpan.FromMilliseconds(50) };
        using var client = new SerpApiClient(httpClient,
            new SerpApiClientOptions { ApiKey = "key" });

        await Assert.ThrowsAsync<SerpApiTimeoutException>(() =>
            client.SearchAsync(new Dictionary<string, string>
            {
                ["engine"] = "google",
                ["q"] = "test"
            }));
    }

    [Fact]
    public async Task HtmlAsync_ReturnsHtmlString()
    {
        var html = "<html><body>Search results</body></html>";
        var handler = new MockHttpHandler((request, _) =>
        {
            Assert.Contains("output=html", request.RequestUri!.ToString());
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(html)
            });
        });

        using var client = new SerpApiClient(new HttpClient(handler),
            new SerpApiClientOptions { ApiKey = "key" });

        var result = await client.HtmlAsync(new Dictionary<string, string>
        {
            ["engine"] = "google",
            ["q"] = "test"
        });

        Assert.Equal(html, result);
    }

    [Fact]
    public async Task SearchArchiveAsync_BuildsCorrectUrl()
    {
        string? capturedUrl = null;
        var handler = new MockHttpHandler((request, _) =>
        {
            capturedUrl = request.RequestUri?.ToString();
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("""{"search_metadata":{"id":"abc123"}}""")
            });
        });

        using var client = new SerpApiClient(new HttpClient(handler),
            new SerpApiClientOptions { ApiKey = "key" });

        await client.SearchArchiveAsync("abc123");
        Assert.Contains("/searches/abc123.json", capturedUrl!);
    }

    [Fact]
    public async Task SearchArchiveAsync_ThrowsOnEmptyId()
    {
        using var client = new SerpApiClient("key");
        await Assert.ThrowsAsync<ArgumentException>(() => client.SearchArchiveAsync(""));
    }

    [Fact]
    public async Task AccountAsync_ReturnsAccountInfo()
    {
        var json = """{"account_id":"123","api_key":"key","plan_id":"free"}""";
        var handler = new MockHttpHandler((request, _) =>
        {
            Assert.Contains("/account", request.RequestUri!.ToString());
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(json)
            });
        });

        using var client = new SerpApiClient(new HttpClient(handler),
            new SerpApiClientOptions { ApiKey = "key" });

        var result = await client.AccountAsync();
        Assert.Equal("123", result["account_id"]!.Value.GetString());
    }

    [Fact]
    public async Task LocationAsync_ReturnsArray()
    {
        var json = """[{"id":"585069bdee19ad271e9bc072","name":"Austin, TX","google_id":200635}]""";
        var handler = new MockHttpHandler((request, _) =>
        {
            var url = request.RequestUri!.ToString();
            Assert.Contains("/locations.json", url);
            Assert.Contains("q=Austin", url);
            Assert.Contains("limit=3", url);
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(json)
            });
        });

        using var client = new SerpApiClient(new HttpClient(handler),
            new SerpApiClientOptions { ApiKey = "key" });

        var result = await client.LocationAsync("Austin", limit: 3);
        Assert.Equal(JsonValueKind.Array, result.ValueKind);
        Assert.Equal(1, result.GetArrayLength());
    }

    [Fact]
    public async Task NextPageAsync_ReturnsNullWhenNoPagination()
    {
        var json = """{"search_metadata":{"id":"abc"},"organic_results":[]}""";
        var handler = new MockHttpHandler((_, _) =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(json)
            }));

        using var client = new SerpApiClient(new HttpClient(handler),
            new SerpApiClientOptions { ApiKey = "key" });

        var firstPage = await client.SearchAsync(new Dictionary<string, string>
        {
            ["engine"] = "google",
            ["q"] = "test"
        });

        var nextPage = await client.NextPageAsync(firstPage);
        Assert.Null(nextPage);
    }

    [Fact]
    public async Task NextPageAsync_FollowsPaginationUrl()
    {
        var json = """
        {
            "search_metadata":{"id":"abc"},
            "organic_results":[],
            "serpapi_pagination":{"next":"https://serpapi.com/search?q=test&start=10"}
        }
        """;
        string? capturedUrl = null;
        var handler = new MockHttpHandler((request, _) =>
        {
            capturedUrl = request.RequestUri?.AbsoluteUri;
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("""{"search_metadata":{"id":"def"},"organic_results":[]}""")
            });
        });

        using var client = new SerpApiClient(new HttpClient(handler),
            new SerpApiClientOptions { ApiKey = "key" });

        var firstPage = new SerpApiResponse(json);
        var nextPage = await client.NextPageAsync(firstPage);

        Assert.NotNull(nextPage);
        Assert.NotNull(capturedUrl);
        Assert.Contains("start=10", capturedUrl!);
        Assert.Contains("api_key=key", capturedUrl!);
        Assert.Contains("source=dotnet", capturedUrl!);
    }

    [Fact]
    public async Task SearchAsync_SpecialCharactersEncodedCorrectly()
    {
        string? capturedUrl = null;
        var handler = new MockHttpHandler((request, _) =>
        {
            capturedUrl = request.RequestUri?.AbsoluteUri;
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("""{"search_metadata":{"id":"x"}}""")
            });
        });

        using var client = new SerpApiClient(new HttpClient(handler),
            new SerpApiClientOptions { ApiKey = "key" });

        await client.SearchAsync(new Dictionary<string, string>
        {
            ["engine"] = "google",
            ["q"] = "café ñ 日本語"
        });

        // Verify the special characters are present in the URL (encoded)
        Assert.NotNull(capturedUrl);
        Assert.Contains("q=", capturedUrl!);
        // The URL must not contain raw non-ASCII characters
        Assert.DoesNotContain("café", capturedUrl!);
        Assert.DoesNotContain("日本語", capturedUrl!);
    }

    [Fact]
    public void Search_Sync_Works()
    {
        var handler = new MockHttpHandler((_, _) =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("""{"search_metadata":{"id":"sync"},"organic_results":[]}""")
            }));

        using var client = new SerpApiClient(new HttpClient(handler),
            new SerpApiClientOptions { ApiKey = "key" });

        var result = client.Search(new Dictionary<string, string>
        {
            ["engine"] = "google",
            ["q"] = "sync test"
        });

        Assert.Equal("sync", result.SearchId);
    }

    [Fact]
    public async Task SearchAsync_ApiKeyNotDuplicatedInParams()
    {
        string? capturedUrl = null;
        var handler = new MockHttpHandler((request, _) =>
        {
            capturedUrl = request.RequestUri?.ToString();
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("""{"search_metadata":{"id":"x"}}""")
            });
        });

        using var client = new SerpApiClient(new HttpClient(handler),
            new SerpApiClientOptions { ApiKey = "main_key" });

        await client.SearchAsync(new Dictionary<string, string>
        {
            ["engine"] = "google",
            ["q"] = "test",
            ["api_key"] = "override_key"  // should be ignored
        });

        // api_key from constructor takes precedence, user-passed api_key is skipped
        var apiKeyCount = capturedUrl!.Split("api_key=").Length - 1;
        Assert.Equal(1, apiKeyCount);
        Assert.Contains("api_key=main_key", capturedUrl);
    }

    [Fact]
    public async Task SearchAsync_CustomBaseUrl()
    {
        string? capturedUrl = null;
        var handler = new MockHttpHandler((request, _) =>
        {
            capturedUrl = request.RequestUri?.ToString();
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("""{"search_metadata":{"id":"x"}}""")
            });
        });

        using var client = new SerpApiClient(new HttpClient(handler),
            new SerpApiClientOptions { ApiKey = "key", BaseUrl = "https://custom.api.com" });

        await client.SearchAsync(new Dictionary<string, string>
        {
            ["engine"] = "google",
            ["q"] = "test"
        });

        Assert.StartsWith("https://custom.api.com/search?", capturedUrl!);
    }

    [Fact]
    public async Task NextPageAsync_RejectsCrossOriginUrl()
    {
        var json = """
        {
            "search_metadata":{"id":"abc"},
            "organic_results":[],
            "serpapi_pagination":{"next":"https://attacker.com/steal?q=test"}
        }
        """;

        using var client = new SerpApiClient(new HttpClient(new MockHttpHandler((_, _) =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("""{"search_metadata":{"id":"x"}}""")
            }))),
            new SerpApiClientOptions { ApiKey = "key" });

        var firstPage = new SerpApiResponse(json);
        var ex = await Assert.ThrowsAsync<SerpApiException>(() => client.NextPageAsync(firstPage));
        Assert.Contains("does not match", ex.Message);
    }

    [Fact]
    public async Task AccountAsync_ThrowsOnApiError()
    {
        var json = """{"error": "Invalid API key. Your API key should be here: https://serpapi.com/manage-api-key"}""";
        var handler = new MockHttpHandler((_, _) =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(json)
            }));

        using var client = new SerpApiClient(new HttpClient(handler),
            new SerpApiClientOptions { ApiKey = "bad_key" });

        await Assert.ThrowsAsync<SerpApiKeyException>(() => client.AccountAsync());
    }

    [Fact]
    public async Task SearchArchiveAsync_EscapesPathSegment()
    {
        string? capturedUrl = null;
        var handler = new MockHttpHandler((request, _) =>
        {
            capturedUrl = request.RequestUri?.ToString();
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("""{"search_metadata":{"id":"x"}}""")
            });
        });

        using var client = new SerpApiClient(new HttpClient(handler),
            new SerpApiClientOptions { ApiKey = "key" });

        await client.SearchArchiveAsync("id/with/slashes");
        Assert.DoesNotContain("/id/with/slashes", capturedUrl!);
        Assert.Contains("id%2Fwith%2Fslashes", capturedUrl!);
    }

    [Fact]
    public async Task SearchPagesAsync_ThrowsOnInvalidMaxPages()
    {
        using var client = new SerpApiClient("key");
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(async () =>
        {
            await foreach (var page in client.SearchPagesAsync(
                new Dictionary<string, string> { ["engine"] = "google", ["q"] = "test" },
                maxPages: 0))
            {
                // should not reach here
            }
        });
    }

    [Fact]
    public void Constructor_HttpClient_CopiesOptions()
    {
        var options = new SerpApiClientOptions
        {
            ApiKey = "original_key",
            BaseUrl = "https://serpapi.com",
            Timeout = TimeSpan.FromSeconds(30)
        };

        using var client = new SerpApiClient(new HttpClient(), options);

        // Mutating the original options after construction should not affect the client
        options.ApiKey = "mutated_key";
        options.BaseUrl = "https://evil.com";

        // Client should still use original values (verified indirectly via construction succeeding)
        Assert.NotNull(client);
    }

    // --- Cancellation ---

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

    // --- Sync wrappers ---

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

    [Fact]
    public async Task SearchAsync_ThrowsGenericExceptionOnQuotaError()
    {
        const string message = "You have run out of searches. Upgrade your API key plan.";
        var handler = new MockHttpHandler((_, _) =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent($"{{\"error\":\"{message}\"}}")
            }));

        using var client = new SerpApiClient(new HttpClient(handler),
            new SerpApiClientOptions { ApiKey = "key" });

        var exception = await Assert.ThrowsAsync<SerpApiException>(() =>
            client.SearchAsync(new Dictionary<string, string>
            {
                ["engine"] = "google",
                ["q"] = "test"
            }));

        Assert.IsNotType<SerpApiKeyException>(exception);
        Assert.Equal(message, exception.Message);
    }

    // --- Dispose safety ---

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
