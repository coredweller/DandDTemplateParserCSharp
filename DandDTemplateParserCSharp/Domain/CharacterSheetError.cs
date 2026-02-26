namespace DandDTemplateParserCSharp.Domain;

public abstract record CharacterSheetError
{
    public sealed record NotFound(Guid Id)           : CharacterSheetError;
    public sealed record ValidationError(string Message) : CharacterSheetError;
    public sealed record DatabaseError(string Message)   : CharacterSheetError;
}
