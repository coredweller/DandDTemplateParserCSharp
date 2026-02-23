namespace DandDTemplateParserCSharp.Domain;

// Abstract record base — nested sealed records expose properties automatically
// via primary constructor (e.g. e.Id, e.Message). Cannot be instantiated directly.
public abstract record TaskError
{
    public sealed record NotFound(TaskId Id) : TaskError;
    public sealed record ValidationError(string Message) : TaskError;
    public sealed record Conflict(string Message) : TaskError;
}
