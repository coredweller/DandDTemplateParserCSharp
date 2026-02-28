using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FluentAssertions;
using Xunit;
using DandDTemplateParserCSharp.Controllers;

namespace DandDTemplateParserCSharp.Tests.Controllers;

[Collection("Integration")]
public sealed class AuthControllerIntegrationTests(ApiFactory factory)
    : IClassFixture<ApiFactory>
{
    [Fact]
    public async Task GetSheets_WithoutToken_Returns401()
    {
        var unauthenticatedClient = factory.CreateClient();

        var response = await unauthenticatedClient.GetAsync("/api/v1/sheets/by-level/5");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetSheets_WithInvalidToken_Returns401()
    {
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", "this.is.not.a.valid.jwt");

        var response = await client.GetAsync("/api/v1/sheets/by-level/5");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task PostToken_WithValidApiSecret_Returns200WithToken()
    {
        var client = factory.CreateClient();
        var tokenRequest = new TokenRequest(TestJwtConstants.ApiSecret);

        var response = await client.PostAsJsonAsync("/api/v1/auth/token", tokenRequest);
        var tokenResponse = await response.Content.ReadFromJsonAsync<TokenResponse>();

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        tokenResponse.Should().NotBeNull();
        tokenResponse!.AccessToken.Should().NotBeNullOrWhiteSpace();
        tokenResponse.ExpiresIn.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task PostToken_WithWrongApiSecret_Returns401()
    {
        var client = factory.CreateClient();
        var tokenRequest = new TokenRequest("wrong-secret-value");

        var response = await client.PostAsJsonAsync("/api/v1/auth/token", tokenRequest);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}
