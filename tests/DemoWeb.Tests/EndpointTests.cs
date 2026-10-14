using System.Net;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace DemoWeb.Tests;

public sealed class EndpointTests(WebApplicationFactory<Program> factory)
    : IClassFixture<WebApplicationFactory<Program>>
{
    [Theory]
    [InlineData("/", "Hello from Azure Container Apps!")]
    [InlineData("/health", "Healthy")]
    public async Task Get_ReturnsExpectedResponse(string path, string expectedBody)
    {
        using var client = factory.CreateClient();
        using var response = await client.GetAsync(path);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(expectedBody, await response.Content.ReadAsStringAsync());
    }
}
