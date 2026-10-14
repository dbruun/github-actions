using Xunit;

namespace DemoWeb.Tests;

public sealed class HomeEndpointTests
{
    [Fact]
    public void GetGreeting_ReturnsExpectedMessage()
    {
        Assert.Equal("Hello from Azure Container Apps!", HomeEndpoint.GetGreeting());
    }
}
