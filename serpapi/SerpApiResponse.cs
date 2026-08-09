using System.Text.Json;

namespace SerpApi;

/// <summary>
/// Wraps a SerpApi JSON response, providing dictionary-like access and convenience properties.
/// </summary>
public sealed class SerpApiResponse : IDisposable
{
    private readonly JsonDocument _document;
    private readonly JsonElement _root;
    private readonly string _rawJson;

    internal SerpApiResponse(string json)
    {
        _rawJson = json;
        _document = JsonDocument.Parse(json);
        _root = _document.RootElement;
    }

    /// <summary>
    /// Access a top-level property by key. Returns null if the key does not exist.
    /// </summary>
    public JsonElement? this[string key]
    {
        get
        {
            if (_root.TryGetProperty(key, out var value))
                return value;
            return null;
        }
    }

    /// <summary>
    /// The search_metadata object from the response.
    /// </summary>
    public JsonElement? SearchMetadata => this["search_metadata"];

    /// <summary>
    /// The search_parameters object from the response.
    /// </summary>
    public JsonElement? SearchParameters => this["search_parameters"];

    /// <summary>
    /// The organic_results array from the response, if present.
    /// </summary>
    public JsonElement? OrganicResults => this["organic_results"];

    /// <summary>
    /// The search ID from search_metadata, used for archive lookups.
    /// </summary>
    public string? SearchId
    {
        get
        {
            var metadata = SearchMetadata;
            if (metadata?.TryGetProperty("id", out var id) == true)
                return id.GetString();
            return null;
        }
    }

    /// <summary>
    /// Pagination info from serpapi_pagination, if present.
    /// </summary>
    public JsonElement? Pagination => this["serpapi_pagination"];

    /// <summary>
    /// URL for the next page of results, if available.
    /// </summary>
    public string? NextPageUrl
    {
        get
        {
            var pagination = Pagination;
            if (pagination?.TryGetProperty("next", out var next) == true)
                return next.GetString();
            return null;
        }
    }

    /// <summary>
    /// Returns the raw JSON string of the response.
    /// </summary>
    public string RawJson => _rawJson;

    // SerpApi responses use snake_case property names (organic_results, search_metadata).
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        PropertyNameCaseInsensitive = true
    };

    /// <summary>
    /// Deserialize the entire response to a specific type.
    /// Snake_case JSON properties map to PascalCase members automatically.
    /// </summary>
    public T? As<T>() => _root.Deserialize<T>(SerializerOptions);

    /// <summary>
    /// Deserialize a specific property to a type.
    /// Snake_case JSON properties map to PascalCase members automatically.
    /// </summary>
    public T? GetProperty<T>(string key)
    {
        var element = this[key];
        if (element == null)
            return default;
        return element.Value.Deserialize<T>(SerializerOptions);
    }

    /// <summary>
    /// Get the root JSON element for advanced traversal.
    /// </summary>
    public JsonElement Root => _root;

    /// <inheritdoc />
    public override string ToString() => _rawJson;

    /// <inheritdoc />
    public void Dispose() => _document.Dispose();
}
