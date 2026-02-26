using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using DandDTemplateParserCSharp.Controllers;
using DandDTemplateParserCSharp.Domain;
using DandDTemplateParserCSharp.Repositories;

namespace DandDTemplateParserCSharp.Tests.Controllers;

// ── Custom factory — replaces Dapper repository with in-memory stub ──────────
public sealed class ApiFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        // Satisfy ValidateOnStart() without a real SQL Server
        builder.ConfigureAppConfiguration((_, config) =>
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Database:ConnectionString"] = "Server=test-stub;Database=test"
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
}

// ── In-memory repository stub ─────────────────────────────────────────────────
public sealed class InMemoryCharacterSheetRepository : ICharacterSheetRepository
{
    private readonly Dictionary<Guid, CharacterSheetRender> _store = [];

    public Task<CharacterSheetRender> SaveAsync(CharacterSheetRender render, CancellationToken ct = default)
    {
        _store[render.Id] = render;
        return Task.FromResult(render);
    }

    public Task<CharacterSheetRender?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => Task.FromResult(_store.GetValueOrDefault(id));
}

// ── Tests ─────────────────────────────────────────────────────────────────────
public sealed class CharacterSheetsControllerIntegrationTests(ApiFactory factory)
    : IClassFixture<ApiFactory>
{
    private readonly HttpClient _client = factory.CreateClient();

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
