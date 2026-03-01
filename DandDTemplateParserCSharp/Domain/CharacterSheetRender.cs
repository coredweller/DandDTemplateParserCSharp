namespace DandDTemplateParserCSharp.Domain;

public sealed class CharacterSheetRender
{
    public Guid     Id            { get; }
    public string   SheetType     { get; }
    public string   CharacterName { get; }
    public int      Level         { get; }
    public string   ResponseHtml  { get; }
    public DateTimeOffset CreatedAt     { get; }

    private CharacterSheetRender(
        Guid id, string sheetType, string characterName,
        int level, string responseHtml, DateTimeOffset createdAt)
    {
        Id            = id;
        SheetType     = sheetType;
        CharacterName = characterName;
        Level         = level;
        ResponseHtml  = responseHtml;
        CreatedAt     = createdAt;
    }

    public static CharacterSheetRender Create(
        string sheetType, string characterName, int level, string responseHtml) =>
        new(Guid.NewGuid(), sheetType, characterName, level, responseHtml, DateTimeOffset.UtcNow);

    public static CharacterSheetRender Reconstitute(
        Guid id, string sheetType, string characterName,
        int level, string responseHtml, DateTimeOffset createdAt) =>
        new(id, sheetType, characterName, level, responseHtml, createdAt);
}
