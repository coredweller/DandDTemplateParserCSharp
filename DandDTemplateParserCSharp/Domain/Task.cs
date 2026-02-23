using System.Text.Json.Serialization;

namespace DandDTemplateParserCSharp.Domain;

// ── Strongly-typed ID — zero boxing, cannot be confused with other Guid IDs ──
public readonly record struct TaskId(Guid Value)
{
    public static TaskId New() => new(Guid.NewGuid());
    public static TaskId From(Guid value) => new(value);

    public static bool TryParse(string input, out TaskId result)
    {
        if (Guid.TryParse(input, out var guid))
        {
            result = new TaskId(guid);
            return true;
        }
        result = default;
        return false;
    }

    public override string ToString() => Value.ToString();
}

// ── Aggregate root ────────────────────────────────────────────────────────────
public sealed class Task
{
    public TaskId   Id        { get; }
    public string   Title     { get; private set; }
    public DateTime CreatedAt { get; }

    // JsonConstructor allows System.Text.Json to deserialize Task in integration tests
    // and API responses. The private constructor enforces factory-method construction
    // in all application code paths.
    [JsonConstructor]
    private Task(TaskId id, string title, DateTime createdAt)
    {
        Id        = id;
        Title     = title;
        CreatedAt = createdAt;
    }

    // Factory — only valid Tasks can be constructed
    public static Task Create(string title) =>
        new(TaskId.New(), title.Trim(), DateTime.UtcNow);

    // Reconstitute from persistence
    public static Task Reconstitute(TaskId id, string title, DateTime createdAt) =>
        new(id, title, createdAt);
}
