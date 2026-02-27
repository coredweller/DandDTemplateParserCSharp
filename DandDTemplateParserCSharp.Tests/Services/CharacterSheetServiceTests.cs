using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Xunit;
using DandDTemplateParserCSharp.Controllers;
using DandDTemplateParserCSharp.Domain;
using DandDTemplateParserCSharp.Repositories;
using DandDTemplateParserCSharp.Services;
using DandDTemplateParserCSharp.Validators;

namespace DandDTemplateParserCSharp.Tests.Services;

public sealed class CharacterSheetServiceTests
{
    private readonly ICharacterSheetRepository _repository = Substitute.For<ICharacterSheetRepository>();
    private readonly CharacterSheetService      _sut;

    public CharacterSheetServiceTests()
    {
        // Default: all repository operations succeed
        _repository.SaveAsync(Arg.Any<CharacterSheetRender>(), Arg.Any<CancellationToken>())
                   .Returns(c => Result<CharacterSheetRender, CharacterSheetError.DatabaseError>.Success(
                       c.Arg<CharacterSheetRender>()));

        _sut = new CharacterSheetService(
            _repository,
            new GeneralSheetRequestValidator(),
            new LegendarySheetRequestValidator(),
            NullLogger<CharacterSheetService>.Instance);
    }

    // ── RenderGeneralAsync ──────────────────────────────────────────────────

    [Fact]
    public async Task RenderGeneralAsync_WithValidRequest_ReturnsSuccess()
    {
        var result = await _sut.RenderGeneralAsync(ValidGeneral());

        result.IsSuccess.Should().BeTrue();
        result.Value.SheetType.Should().Be("general");
        result.Value.CharacterName.Should().Be("Aldric Stormforge");
        result.Value.Level.Should().Be(5);
    }

    [Fact]
    public async Task RenderGeneralAsync_WithValidRequest_SavesOneRenderToRepository()
    {
        await _sut.RenderGeneralAsync(ValidGeneral());

        await _repository.Received(1).SaveAsync(
            Arg.Is<CharacterSheetRender>(r =>
                r.SheetType == "general" && r.CharacterName == "Aldric Stormforge"),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RenderGeneralAsync_WithValidRequest_HtmlContainsCharacterName()
    {
        var result = await _sut.RenderGeneralAsync(ValidGeneral());

        result.Value.ResponseHtml.Should().Contain("Aldric Stormforge");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task RenderGeneralAsync_WithBlankCharacterName_ReturnsValidationError(string name)
    {
        var request = new GeneralSheetRequest { CharacterName = name, Level = 5 };

        var result = await _sut.RenderGeneralAsync(request);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().BeOfType<CharacterSheetError.ValidationError>();
        await _repository.DidNotReceive().SaveAsync(Arg.Any<CharacterSheetRender>(), Arg.Any<CancellationToken>());
    }

    [Theory]
    [InlineData(0)]
    [InlineData(21)]
    [InlineData(-1)]
    public async Task RenderGeneralAsync_WithInvalidLevel_ReturnsValidationError(int level)
    {
        var request = new GeneralSheetRequest { CharacterName = "Hero", Level = level };

        var result = await _sut.RenderGeneralAsync(request);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().BeOfType<CharacterSheetError.ValidationError>();
        await _repository.DidNotReceive().SaveAsync(Arg.Any<CharacterSheetRender>(), Arg.Any<CancellationToken>());
    }

    // ── RenderLegendaryAsync ────────────────────────────────────────────────

    [Fact]
    public async Task RenderLegendaryAsync_WithValidRequest_ReturnsSuccessWithCorrectSheetType()
    {
        var result = await _sut.RenderLegendaryAsync(ValidLegendary());

        result.IsSuccess.Should().BeTrue();
        result.Value.SheetType.Should().Be("legendary");
        result.Value.CharacterName.Should().Be("Ancient Dragon");
    }

    [Fact]
    public async Task RenderLegendaryAsync_WithValidRequest_HtmlContainsLegendaryBadge()
    {
        var result = await _sut.RenderLegendaryAsync(ValidLegendary());

        result.Value.ResponseHtml.Should().Contain("Legendary Creature");
    }

    [Fact]
    public async Task RenderLegendaryAsync_WithBlankCharacterName_ReturnsValidationError()
    {
        var request = new LegendarySheetRequest { CharacterName = "", Level = 20 };

        var result = await _sut.RenderLegendaryAsync(request);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().BeOfType<CharacterSheetError.ValidationError>();
        await _repository.DidNotReceive().SaveAsync(Arg.Any<CharacterSheetRender>(), Arg.Any<CancellationToken>());
    }

    // ── GetByIdAsync ────────────────────────────────────────────────────────

    [Fact]
    public async Task GetByIdAsync_WhenRenderExists_ReturnsSuccess()
    {
        var render = CharacterSheetRender.Create("general", "Hero", 5, "<html/>");
        _repository.GetByIdAsync(render.Id, Arg.Any<CancellationToken>())
                   .Returns(Result<CharacterSheetRender?, CharacterSheetError.DatabaseError>.Success(render));

        var result = await _sut.GetByIdAsync(render.Id);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(render);
    }

    [Fact]
    public async Task GetByIdAsync_WhenRenderMissing_ReturnsNotFoundWithCorrectId()
    {
        var id = Guid.NewGuid();
        _repository.GetByIdAsync(id, Arg.Any<CancellationToken>())
                   .Returns(Result<CharacterSheetRender?, CharacterSheetError.DatabaseError>.Success(null));

        var result = await _sut.GetByIdAsync(id);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().BeOfType<CharacterSheetError.NotFound>();
        result.Error.As<CharacterSheetError.NotFound>().Id.Should().Be(id);
    }

    // ── GetByLevelAsync ─────────────────────────────────────────────────────

    [Fact]
    public async Task GetByLevelAsync_WithValidLevel_ReturnsSuccess()
    {
        _repository.GetByLevelAsync(5, Arg.Any<CancellationToken>())
                   .Returns(Result<IReadOnlyList<CharacterSheetSummary>, CharacterSheetError.DatabaseError>.Success([]));

        var result = await _sut.GetByLevelAsync(5);

        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task GetByLevelAsync_WithValidLevel_ReturnsRepositoryResults()
    {
        var summary = new CharacterSheetSummary(Guid.NewGuid(), "general", "Hero", 5, DateTime.UtcNow);
        _repository.GetByLevelAsync(5, Arg.Any<CancellationToken>())
                   .Returns(Result<IReadOnlyList<CharacterSheetSummary>, CharacterSheetError.DatabaseError>.Success([summary]));

        var result = await _sut.GetByLevelAsync(5);

        result.Value.Should().ContainSingle(s => s.Id == summary.Id);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(21)]
    [InlineData(-1)]
    public async Task GetByLevelAsync_WithInvalidLevel_ReturnsValidationError(int level)
    {
        var result = await _sut.GetByLevelAsync(level);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().BeOfType<CharacterSheetError.ValidationError>();
        await _repository.DidNotReceive().GetByLevelAsync(Arg.Any<int>(), Arg.Any<CancellationToken>());
    }

    // ── GetBySheetTypeAsync ─────────────────────────────────────────────────

    [Theory]
    [InlineData("general")]
    [InlineData("legendary")]
    public async Task GetBySheetTypeAsync_WithValidSheetType_ReturnsSuccess(string sheetType)
    {
        _repository.GetBySheetTypeAsync(sheetType, Arg.Any<CancellationToken>())
                   .Returns(Result<IReadOnlyList<CharacterSheetSummary>, CharacterSheetError.DatabaseError>.Success([]));

        var result = await _sut.GetBySheetTypeAsync(sheetType);

        result.IsSuccess.Should().BeTrue();
    }

    [Theory]
    [InlineData("GENERAL",   "general")]
    [InlineData("LEGENDARY", "legendary")]
    public async Task GetBySheetTypeAsync_NormalizesSheetTypeToLowercase(string input, string normalized)
    {
        _repository.GetBySheetTypeAsync(normalized, Arg.Any<CancellationToken>())
                   .Returns(Result<IReadOnlyList<CharacterSheetSummary>, CharacterSheetError.DatabaseError>.Success([]));

        var result = await _sut.GetBySheetTypeAsync(input);

        result.IsSuccess.Should().BeTrue();
        await _repository.Received(1).GetBySheetTypeAsync(normalized, Arg.Any<CancellationToken>());
    }

    [Theory]
    [InlineData("invalid")]
    [InlineData("monster")]
    [InlineData("")]
    public async Task GetBySheetTypeAsync_WithInvalidSheetType_ReturnsValidationError(string sheetType)
    {
        var result = await _sut.GetBySheetTypeAsync(sheetType);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().BeOfType<CharacterSheetError.ValidationError>();
        await _repository.DidNotReceive().GetBySheetTypeAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    // ── DatabaseError (repository returns failure) ───────────────────────────

    [Fact]
    public async Task RenderGeneralAsync_WhenRepositoryFails_ReturnsDatabaseError()
    {
        _repository.SaveAsync(Arg.Any<CharacterSheetRender>(), Arg.Any<CancellationToken>())
                   .Returns(Result<CharacterSheetRender, CharacterSheetError.DatabaseError>.Failure(
                       new CharacterSheetError.DatabaseError("A database error occurred.")));

        var result = await _sut.RenderGeneralAsync(ValidGeneral());

        result.IsFailure.Should().BeTrue();
        result.Error.Should().BeOfType<CharacterSheetError.DatabaseError>();
    }

    [Fact]
    public async Task RenderLegendaryAsync_WhenRepositoryFails_ReturnsDatabaseError()
    {
        _repository.SaveAsync(Arg.Any<CharacterSheetRender>(), Arg.Any<CancellationToken>())
                   .Returns(Result<CharacterSheetRender, CharacterSheetError.DatabaseError>.Failure(
                       new CharacterSheetError.DatabaseError("A database error occurred.")));

        var result = await _sut.RenderLegendaryAsync(ValidLegendary());

        result.IsFailure.Should().BeTrue();
        result.Error.Should().BeOfType<CharacterSheetError.DatabaseError>();
    }

    [Fact]
    public async Task GetByIdAsync_WhenRepositoryFails_ReturnsDatabaseError()
    {
        _repository.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
                   .Returns(Result<CharacterSheetRender?, CharacterSheetError.DatabaseError>.Failure(
                       new CharacterSheetError.DatabaseError("A database error occurred.")));

        var result = await _sut.GetByIdAsync(Guid.NewGuid());

        result.IsFailure.Should().BeTrue();
        result.Error.Should().BeOfType<CharacterSheetError.DatabaseError>();
    }

    [Fact]
    public async Task GetByLevelAsync_WhenRepositoryFails_ReturnsDatabaseError()
    {
        _repository.GetByLevelAsync(5, Arg.Any<CancellationToken>())
                   .Returns(Result<IReadOnlyList<CharacterSheetSummary>, CharacterSheetError.DatabaseError>.Failure(
                       new CharacterSheetError.DatabaseError("A database error occurred.")));

        var result = await _sut.GetByLevelAsync(5);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().BeOfType<CharacterSheetError.DatabaseError>();
    }

    [Fact]
    public async Task GetBySheetTypeAsync_WhenRepositoryFails_ReturnsDatabaseError()
    {
        _repository.GetBySheetTypeAsync("general", Arg.Any<CancellationToken>())
                   .Returns(Result<IReadOnlyList<CharacterSheetSummary>, CharacterSheetError.DatabaseError>.Failure(
                       new CharacterSheetError.DatabaseError("A database error occurred.")));

        var result = await _sut.GetBySheetTypeAsync("general");

        result.IsFailure.Should().BeTrue();
        result.Error.Should().BeOfType<CharacterSheetError.DatabaseError>();
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
