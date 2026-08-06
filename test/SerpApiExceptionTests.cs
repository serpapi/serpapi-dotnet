namespace SerpApi.Tests;

public class SerpApiExceptionTests
{
    [Fact]
    public void SerpApiException_HasMessage()
    {
        var ex = new SerpApiException("something went wrong");
        Assert.Equal("something went wrong", ex.Message);
    }

    [Fact]
    public void SerpApiHttpException_HasStatusCode()
    {
        var ex = new SerpApiHttpException(429, "Rate limited");
        Assert.Equal(429, ex.StatusCode);
        Assert.Equal("Rate limited", ex.Message);
        Assert.IsAssignableFrom<SerpApiException>(ex);
    }

    [Fact]
    public void SerpApiKeyException_IsAssignableFromBase()
    {
        var ex = new SerpApiKeyException("Invalid key");
        Assert.IsAssignableFrom<SerpApiException>(ex);
    }

    [Fact]
    public void SerpApiTimeoutException_HasInnerException()
    {
        var inner = new TaskCanceledException();
        var ex = new SerpApiTimeoutException("timed out", inner);
        Assert.Equal(inner, ex.InnerException);
        Assert.IsAssignableFrom<SerpApiException>(ex);
    }
}
