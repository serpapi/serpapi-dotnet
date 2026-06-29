using System.Text.Json;

namespace SerpApi.Tests;

public class SerpApiResponseTests
{
    [Fact]
    public void Indexer_ReturnsProperty()
    {
        var response = new SerpApiResponse("""{"organic_results":[{"title":"Hello"}]}""");
        Assert.NotNull(response["organic_results"]);
        Assert.Equal(JsonValueKind.Array, response["organic_results"]!.Value.ValueKind);
    }

    [Fact]
    public void Indexer_ReturnsNullForMissingProperty()
    {
        var response = new SerpApiResponse("""{"search_metadata":{"id":"x"}}""");
        Assert.Null(response["nonexistent"]);
    }

    [Fact]
    public void SearchMetadata_Works()
    {
        var response = new SerpApiResponse("""{"search_metadata":{"id":"abc","status":"Success"}}""");
        Assert.NotNull(response.SearchMetadata);
        Assert.Equal("abc", response.SearchId);
    }

    [Fact]
    public void OrganicResults_ParsesArray()
    {
        var json = """
        {
            "search_metadata":{"id":"x"},
            "organic_results":[
                {"position":1,"title":"First"},
                {"position":2,"title":"Second"}
            ]
        }
        """;
        var response = new SerpApiResponse(json);
        Assert.Equal(2, response.OrganicResults!.Value.GetArrayLength());
    }

    [Fact]
    public void NextPageUrl_ReturnsPaginationUrl()
    {
        var json = """
        {
            "search_metadata":{"id":"x"},
            "serpapi_pagination":{"next":"https://serpapi.com/search?start=10"}
        }
        """;
        var response = new SerpApiResponse(json);
        Assert.Equal("https://serpapi.com/search?start=10", response.NextPageUrl);
    }

    [Fact]
    public void NextPageUrl_ReturnsNullWhenNoPagination()
    {
        var response = new SerpApiResponse("""{"search_metadata":{"id":"x"}}""");
        Assert.Null(response.NextPageUrl);
    }

    [Fact]
    public void RawJson_ReturnsOriginal()
    {
        var json = """{"key":"value"}""";
        var response = new SerpApiResponse(json);
        Assert.Equal(json, response.RawJson);
    }

    [Fact]
    public void GetProperty_DeserializesToType()
    {
        var json = """{"search_metadata":{"id":"x","status":"Success"},"organic_results":[{"title":"A"}]}""";
        var response = new SerpApiResponse(json);
        var results = response.GetProperty<List<Dictionary<string, string>>>("organic_results");
        Assert.NotNull(results);
        Assert.Single(results!);
        Assert.Equal("A", results[0]["title"]);
    }

    [Fact]
    public void As_DeserializesEntireResponse()
    {
        var json = """{"search_metadata":{"id":"x"},"organic_results":[]}""";
        var response = new SerpApiResponse(json);
        var dict = response.As<Dictionary<string, JsonElement>>();
        Assert.NotNull(dict);
        Assert.True(dict!.ContainsKey("search_metadata"));
    }

    [Fact]
    public void ToString_ReturnsJson()
    {
        var json = """{"a":"b"}""";
        var response = new SerpApiResponse(json);
        Assert.Equal(json, response.ToString());
    }

    [Fact]
    public void SearchId_NullWhenNoMetadata()
    {
        var response = new SerpApiResponse("""{"organic_results":[]}""");
        Assert.Null(response.SearchId);
    }

    [Fact]
    public void SearchMetadata_NullWhenMissing()
    {
        var response = new SerpApiResponse("""{"organic_results":[]}""");
        Assert.Null(response.SearchMetadata);
    }

    [Fact]
    public void GetProperty_ReturnsDefaultForMissingKey()
    {
        var response = new SerpApiResponse("""{"search_metadata":{"id":"x"}}""");
        var result = response.GetProperty<List<string>>("nonexistent");
        Assert.Null(result);
    }

    [Fact]
    public void DisposeMultipleTimes_DoesNotThrow()
    {
        var response = new SerpApiResponse("""{"a":"b"}""");
        response.Dispose();
        var ex = Record.Exception(() => response.Dispose());
        Assert.Null(ex);
    }
}
