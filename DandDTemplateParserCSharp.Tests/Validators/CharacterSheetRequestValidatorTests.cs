using DandDTemplateParserCSharp.Domain;
using DandDTemplateParserCSharp.Validators;
using FluentAssertions;
using Xunit;

namespace DandDTemplateParserCSharp.Tests.Validators;

public class AbilityScoreValidatorTests
{
    private readonly AbilityScoreValidator _sut = new();

    [Theory]
    [InlineData(0)]
    [InlineData(31)]
    public void Validate_ScoreOutOfRange_ShouldBeInvalid(int score)
    {
        var result = _sut.Validate(new AbilityScore(score));

        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle()
            .Which.ErrorMessage.Should().Be("Ability score must be between 1 and 30.");
    }

    [Theory]
    [InlineData(1)]
    [InlineData(15)]
    [InlineData(30)]
    public void Validate_ScoreInRange_ShouldBeValid(int score)
    {
        var result = _sut.Validate(new AbilityScore(score));

        result.IsValid.Should().BeTrue();
    }
}

public class GeneralSheetRequestAbilityScoreValidationTests
{
    private readonly GeneralSheetRequestValidator _sut = new();

    private static GeneralSheetRequest CreateValidRequest(AbilityScores? abilityScores = null) =>
        new()
        {
            CharacterName = "TestCharacter",
            Level = 5,
            AbilityScores = abilityScores
        };

    [Fact]
    public void Validate_AbilityScoresNull_ShouldBeValid()
    {
        var request = CreateValidRequest(abilityScores: null);

        var result = _sut.Validate(request);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_StrengthScoreOutOfRange_ShouldBeInvalid()
    {
        var request = CreateValidRequest(
            new AbilityScores(
                Strength: new AbilityScore(0),
                Dexterity: null,
                Constitution: null,
                Intelligence: null,
                Wisdom: null,
                Charisma: null));

        var result = _sut.Validate(request);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e =>
            e.PropertyName == "AbilityScores.Strength.Score");
    }

    [Fact]
    public void Validate_StrengthScoreInRange_ShouldBeValid()
    {
        var request = CreateValidRequest(
            new AbilityScores(
                Strength: new AbilityScore(15),
                Dexterity: null,
                Constitution: null,
                Intelligence: null,
                Wisdom: null,
                Charisma: null));

        var result = _sut.Validate(request);

        result.IsValid.Should().BeTrue();
    }
}
