using DandDTemplateParserCSharp.Domain;

namespace DandDTemplateParserCSharp.Services;

public interface ITaskService
{
    Task<IReadOnlyList<Domain.Task>> ListAllAsync(CancellationToken ct = default);
    Task<Result<Domain.Task>>        GetByIdAsync(TaskId id, CancellationToken ct = default);
    Task<Result<Domain.Task>>        CreateAsync(string title, CancellationToken ct = default);
    Task<Result<bool>>               DeleteAsync(TaskId id, CancellationToken ct = default);
}
