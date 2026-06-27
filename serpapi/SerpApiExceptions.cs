namespace SerpApi;

/// <summary>
/// Base exception for all SerpApi errors.
/// </summary>
public class SerpApiException : Exception
{
    /// <inheritdoc />
    public SerpApiException(string message) : base(message) { }
    /// <inheritdoc />
    public SerpApiException(string message, Exception innerException) : base(message, innerException) { }
}

/// <summary>
/// Thrown when the API returns an HTTP error response.
/// </summary>
public class SerpApiHttpException : SerpApiException
{
    /// <summary>
    /// HTTP status code returned by the API.
    /// </summary>
    public int StatusCode { get; }

    /// <inheritdoc />
    public SerpApiHttpException(int statusCode, string message)
        : base(message)
    {
        StatusCode = statusCode;
    }
}

/// <summary>
/// Thrown when the API key is missing or invalid.
/// </summary>
public class SerpApiKeyException : SerpApiException
{
    /// <inheritdoc />
    public SerpApiKeyException(string message) : base(message) { }
}

/// <summary>
/// Thrown when an HTTP request times out.
/// </summary>
public class SerpApiTimeoutException : SerpApiException
{
    /// <inheritdoc />
    public SerpApiTimeoutException(string message, Exception innerException)
        : base(message, innerException) { }
}
