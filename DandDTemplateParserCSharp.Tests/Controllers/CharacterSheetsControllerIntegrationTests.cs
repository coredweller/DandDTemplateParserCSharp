using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text;
using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using Xunit;
using DandDTemplateParserCSharp.Controllers;
using DandDTemplateParserCSharp.Domain;
using DandDTemplateParserCSharp.Repositories;

namespace DandDTemplateParserCSharp.Tests.Controllers;

// ── Test constants ────────────────────────────────────────────────────────────
internal static class TestJwtConstants
{
    internal const string SigningKey = "test-signing-key-exactly-32-chars!!";
    internal const string ApiSecret = "test-api-secret";
    internal const string Issuer = "DandDTemplateParserCSharp";
    internal const string Audience = "DandDTemplateParserCSharp";
}

// ── Custom factory — replaces Dapper repository with in-memory stub ──────────
public sealed class ApiFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        // Satisfy ValidateOnStart() without a real SQL Server or real JWT secrets
        builder.ConfigureAppConfiguration((_, config) =>
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Database:ConnectionString"] = "Server=test-stub;Database=test",
                ["Jwt:SigningKey"]            = TestJwtConstants.SigningKey,
                ["Jwt:ApiSecret"]            = TestJwtConstants.ApiSecret,
                ["Jwt:Issuer"]               = TestJwtConstants.Issuer,
                ["Jwt:Audience"]             = TestJwtConstants.Audience,
                ["Jwt:ExpirationMinutes"]    = "60"
            }));

        builder.ConfigureServices(services =>
        {
            var descriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(ICharacterSheetRepository));
            if (descriptor is not null)
                services.Remove(descriptor);

            // Singleton so renders persists across requests within a single test
            services.AddSingleton<ICharacterSheetRepository, InMemoryCharacterSheetRepository>();
        });
    }

    /// <summary>Creates a valid JWT for integration tests using the test signing key.</summary>
    internal static string CreateTestToken()
    {
        var keyBytes = Encoding.UTF8.GetBytes(TestJwtConstants.SigningKey);
        var securityKey = new SymmetricSecurityKey(keyBytes);
        var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

        var now = DateTime.UtcNow;

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, "api-user"),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        var token = new JwtSecurityToken(
            issuer: TestJwtConstants.Issuer,
            audience: TestJwtConstants.Audience,
            claims: claims,
            notBefore: now,
            expires: now.AddMinutes(60),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}

// ── In-memory repository stub ─────────────────────────────────────────────────
public sealed class InMemoryCharacterSheetRepository : ICharacterSheetRepository
{
    private readonly Dictionary<Guid, CharacterSheetRender> _store = [];

    public Task<Result<CharacterSheetRender, CharacterSheetError.DatabaseError>> SaveAsync(
        CharacterSheetRender render, CancellationToken ct = default)
    {
        _store[render.Id] = render;
        return Task.FromResult(Result<CharacterSheetRender, CharacterSheetError.DatabaseError>.Success(render));
    }

    public Task<Result<CharacterSheetRender?, CharacterSheetError.DatabaseError>> GetByIdAsync(
        Guid id, CancellationToken ct = default)
        => Task.FromResult(Result<CharacterSheetRender?, CharacterSheetError.DatabaseError>.Success(
            _store.GetValueOrDefault(id)));

    public Task<Result<IReadOnlyList<CharacterSheetSummary>, CharacterSheetError.DatabaseError>> GetByLevelAsync(
        int level, PageRequest page, CancellationToken ct = default)
    {
        IReadOnlyList<CharacterSheetSummary> results = _store.Values
            .Where(r => r.Level == level)
            .OrderByDescending(r => r.CreatedAt)
            .Skip(page.Offset)
            .Take(page.PageSize)
            .Select(r => new CharacterSheetSummary(r.Id, r.SheetType, r.CharacterName, r.Level, r.CreatedAt))
            .ToList();
        return Task.FromResult(Result<IReadOnlyList<CharacterSheetSummary>, CharacterSheetError.DatabaseError>.Success(results));
    }

    public Task<Result<IReadOnlyList<CharacterSheetSummary>, CharacterSheetError.DatabaseError>> GetBySheetTypeAsync(
        string sheetType, PageRequest page, CancellationToken ct = default)
    {
        IReadOnlyList<CharacterSheetSummary> results = _store.Values
            .Where(r => r.SheetType == sheetType)
            .OrderByDescending(r => r.CreatedAt)
            .Skip(page.Offset)
            .Take(page.PageSize)
            .Select(r => new CharacterSheetSummary(r.Id, r.SheetType, r.CharacterName, r.Level, r.CreatedAt))
            .ToList();
        return Task.FromResult(Result<IReadOnlyList<CharacterSheetSummary>, CharacterSheetError.DatabaseError>.Success(results));
    }
}

// ── Sequences integration test classes so WebApplicationFactory instances
//    are not constructed in parallel (HostFactoryResolver is not re-entrant).
[CollectionDefinition("Integration")]
public sealed class IntegrationFixtures;

// ── Character Sheets Integration Tests ───────────────────────────────────────
[Collection("Integration")]
public sealed class CharacterSheetsControllerIntegrationTests(ApiFactory factory)
    : IClassFixture<ApiFactory>
{
    private readonly HttpClient _client = CreateAuthenticatedClient(factory);

    private static HttpClient CreateAuthenticatedClient(ApiFactory factory)
    {
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", ApiFactory.CreateTestToken());
        return client;
    }

    // ── POST /api/v1/sheets/general ─────────────────────────────────────────

    [Fact]
    public async Task PostGeneral_WithValidRequest_Returns201()
    {
        var response = await _client.PostAsJsonAsync("/api/v1/sheets/general", ValidGeneral());

        response.StatusCode.Should().Be(HttpStatusCode.Created);
    }

    [Fact]
    public async Task PostGeneral_WithValidRequest_ReturnsTextHtml()
    {
        var response = await _client.PostAsJsonAsync("/api/v1/sheets/general", ValidGeneral());

        response.Content.Headers.ContentType!.MediaType.Should().Be("text/html");
    }

    [Fact]
    public async Task PostGeneral_WithValidRequest_HtmlBodyContainsCharacterName()
    {
        var response = await _client.PostAsJsonAsync("/api/v1/sheets/general", ValidGeneral());
        var html = await response.Content.ReadAsStringAsync();

        html.Should().Contain("Aldric Stormforge");
    }

    [Fact]
    public async Task PostGeneral_WithValidRequest_ReturnsLocationHeader()
    {
        var response = await _client.PostAsJsonAsync("/api/v1/sheets/general", ValidGeneral());

        response.Headers.Location.Should().NotBeNull();
    }

    [Fact]
    public async Task PostGeneral_WithBlankCharacterName_Returns400()
    {
        var request = new GeneralSheetRequest { CharacterName = "", Level = 5 };

        var response = await _client.PostAsJsonAsync("/api/v1/sheets/general", request);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(21)]
    public async Task PostGeneral_WithInvalidLevel_Returns400(int level)
    {
        var request = new GeneralSheetRequest { CharacterName = "Hero", Level = level };

        var response = await _client.PostAsJsonAsync("/api/v1/sheets/general", request);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    // ── POST /api/v1/sheets/legendary ───────────────────────────────────────

    [Fact]
    public async Task PostLegendary_WithValidRequest_Returns201AndTextHtml()
    {
        var response = await _client.PostAsJsonAsync("/api/v1/sheets/legendary", ValidLegendary());

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        response.Content.Headers.ContentType!.MediaType.Should().Be("text/html");
    }

    [Fact]
    public async Task PostLegendary_WithLegendaryActions_HtmlContainsSectionHeading()
    {
        var request = ValidLegendary() with
        {
            LegendaryActions = new LegendaryActions("3", new Dictionary<string, string>
            {
                ["Claw"] = "The dragon makes one claw attack."
            }),
            ChallengeRating = "24"
        };

        var response = await _client.PostAsJsonAsync("/api/v1/sheets/legendary", request);
        var html = await response.Content.ReadAsStringAsync();

        html.Should().Contain("Legendary Actions");
        html.Should().Contain("Ancient Dragon");
    }

    // ── GET /api/v1/sheets/{id} ─────────────────────────────────────────────

    [Fact]
    public async Task GetById_AfterPost_Returns200WithSameHtml()
    {
        var postResponse = await _client.PostAsJsonAsync("/api/v1/sheets/general", ValidGeneral());
        var location     = postResponse.Headers.Location!;

        var getResponse = await _client.GetAsync(location);
        var html        = await getResponse.Content.ReadAsStringAsync();

        getResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        getResponse.Content.Headers.ContentType!.MediaType.Should().Be("text/html");
        html.Should().Contain("Aldric Stormforge");
    }

    [Fact]
    public async Task GetById_WithUnknownId_Returns404()
    {
        var response = await _client.GetAsync($"/api/v1/sheets/{Guid.NewGuid()}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // ── GET /api/v1/sheets/by-level/{level} ─────────────────────────────────

    [Fact]
    public async Task GetByLevel_AfterPost_ReturnsMatchingRender()
    {
        var request = new GeneralSheetRequest { CharacterName = "Level7Hero", Level = 7 };
        await _client.PostAsJsonAsync("/api/v1/sheets/general", request);

        var response  = await _client.GetAsync("/api/v1/sheets/by-level/7");
        var summaries = await response.Content.ReadFromJsonAsync<List<CharacterSheetSummary>>();

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        summaries.Should().Contain(s => s.CharacterName == "Level7Hero" && s.Level == 7);
    }

    [Fact]
    public async Task GetByLevel_ReturnsEmptyListWhenNoMatches()
    {
        var response  = await _client.GetAsync("/api/v1/sheets/by-level/13");
        var summaries = await response.Content.ReadFromJsonAsync<List<CharacterSheetSummary>>();

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        summaries.Should().BeEmpty();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(21)]
    public async Task GetByLevel_WithInvalidLevel_Returns400(int level)
    {
        var response = await _client.GetAsync($"/api/v1/sheets/by-level/{level}");

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    // ── GET /api/v1/sheets/by-type/{sheetType} ──────────────────────────────

    [Fact]
    public async Task GetBySheetType_WithGeneralType_ReturnsOnlyGeneralSheets()
    {
        await _client.PostAsJsonAsync("/api/v1/sheets/general", ValidGeneral());

        var response  = await _client.GetAsync("/api/v1/sheets/by-type/general");
        var summaries = await response.Content.ReadFromJsonAsync<List<CharacterSheetSummary>>();

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        summaries.Should().NotBeEmpty();
        summaries.Should().OnlyContain(s => s.SheetType == "general");
    }

    [Fact]
    public async Task GetBySheetType_IsCaseInsensitive()
    {
        var response = await _client.GetAsync("/api/v1/sheets/by-type/GENERAL");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetBySheetType_WithInvalidType_Returns400()
    {
        var response = await _client.GetAsync("/api/v1/sheets/by-type/invalid");

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    // ── Helpers ─────────────────────────────────────────────────────────────

    private static GeneralSheetRequest ValidGeneral() => new()
    {
        CharacterName = "Aldric Stormforge",
        Level         = 5,
        Race          = "Mountain Dwarf",
        Class         = "Fighter"
    };

    private static LegendarySheetRequest ValidLegendary() => new()
    {
        CharacterName = "Ancient Dragon",
        Level         = 20
    };
}

