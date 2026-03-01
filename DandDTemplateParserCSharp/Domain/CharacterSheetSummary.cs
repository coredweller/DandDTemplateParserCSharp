namespace DandDTemplateParserCSharp.Domain;

/// <summary>Metadata-only summary — no HTML payload — used in list/search results.</summary>
public sealed record CharacterSheetSummary(
    Guid     Id,
    string   SheetType,
    string   CharacterName,
    int      Level,
    DateTimeOffset CreatedAt);
