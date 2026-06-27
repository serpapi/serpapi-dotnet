using Microsoft.Extensions.DependencyInjection;

namespace SerpApi.Tests;

public class DependencyInjectionTests
{
    [Fact]
    public void AddSerpApi_RegistersClient()
    {
        var services = new ServiceCollection();
        services.AddSerpApi(options =>
        {
            options.ApiKey = "test_key";
            options.Timeout = TimeSpan.FromSeconds(30);
        });

        var provider = services.BuildServiceProvider();
        var client = provider.GetRequiredService<SerpApiClient>();
        Assert.NotNull(client);
    }

    [Fact]
    public void AddSerpApi_ReturnsHttpClientBuilder()
    {
        var services = new ServiceCollection();
        var builder = services.AddSerpApi(options =>
        {
            options.ApiKey = "test_key";
        });

        Assert.NotNull(builder);
    }
}
