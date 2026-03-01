using DandDTemplateParserCSharp.Domain;
using FluentAssertions;
using Xunit;

namespace DandDTemplateParserCSharp.Tests.Domain;

public class AbilityScoreTests
{
    [Theory]
    [InlineData(10, "+0")]
    [InlineData(11, "+0")]
    [InlineData(12, "+1")]
    [InlineData(20, "+5")]
    [InlineData(9, "-1")]
    [InlineData(8, "-1")]
    [InlineData(1, "-5")]
    public void Modifier_ShouldBeComputedFromScore(int score, string expectedModifier)
    {
        var abilityScore = new AbilityScore(score);

        abilityScore.Modifier.Should().Be(expectedModifier);
    }
}
