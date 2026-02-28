namespace DandDTemplateParserCSharp.Domain;

public sealed record AbilityScore(int Score, string Modifier);

public sealed record AbilityScores(
    AbilityScore? Strength,
    AbilityScore? Dexterity,
    AbilityScore? Constitution,
    AbilityScore? Intelligence,
    AbilityScore? Wisdom,
    AbilityScore? Charisma);

public sealed record Equipment(string? Armor, string? Weapons, string? Other);

public sealed record MythicTrait(string? Name, string? Description);

public sealed record LegendaryActions(
    string? LegendaryActionUses,
    Dictionary<string, string>? Options);

/// <summary>General character sheet — matches blankGeneralTemplate.json.</summary>
public record GeneralSheetRequest
{
    public required string                      CharacterName  { get; init; }
    public required int                         Level          { get; init; }
    public string?                              Race           { get; init; }
    public string?                              Class          { get; init; }
    public string?                              Alignment      { get; init; }
    public string?                              HP             { get; init; }
    public int                                  AC             { get; init; }
    public string?                              Speed          { get; init; }
    public AbilityScores?                       AbilityScores  { get; init; }
    public Dictionary<string, string>?          SavingThrows   { get; init; }
    public Dictionary<string, string>?          Skills         { get; init; }
    public string?                              Senses         { get; init; }
    public string?                              Languages      { get; init; }
    public Dictionary<string, string>?          SpecialTraits  { get; init; }
    public Dictionary<string, string>?          Actions        { get; init; }
    public Equipment?                           Equipment      { get; init; }
    public string?                              Notes          { get; init; }
}

/// <summary>Legendary character sheet — superset of GeneralSheetRequest.</summary>
public sealed record LegendarySheetRequest : GeneralSheetRequest
{
    public string?                              DamageResistances   { get; init; }
    public string?                              DamageImmunities    { get; init; }
    public string?                              ConditionImmunities { get; init; }
    public string?                              ChallengeRating     { get; init; }
    public string?                              ProficiencyBonus    { get; init; }
    public Dictionary<string, string>?          BonusActions        { get; init; }
    public Dictionary<string, string>?          Reactions           { get; init; }
    public Dictionary<string, string>?          LegendaryTraits     { get; init; }
    public LegendaryActions?                    LegendaryActions    { get; init; }
    public MythicTrait?                         MythicTrait         { get; init; }
    public Dictionary<string, string>?          LairActions         { get; init; }
    public List<string>?                        RegionalEffects     { get; init; }
}
