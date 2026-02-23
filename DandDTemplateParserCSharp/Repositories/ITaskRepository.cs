using DandDTemplateParserCSharp.Domain;

namespace DandDTemplateParserCSharp.Repositories;

public interface ITaskRepository
{
    Task<IReadOnlyList<Domain.Task>> GetAllAsync(CancellationToken ct = default);
    Task<Domain.Task?> GetByIdAsync(TaskId id, CancellationToken ct = default);
    Task<Domain.Task>  SaveAsync(Domain.Task task, CancellationToken ct = default);
    Task<bool>         DeleteAsync(TaskId id, CancellationToken ct = default);
}
