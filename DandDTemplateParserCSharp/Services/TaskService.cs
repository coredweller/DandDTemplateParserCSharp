using DandDTemplateParserCSharp.Domain;
using DandDTemplateParserCSharp.Repositories;

namespace DandDTemplateParserCSharp.Services;

public sealed class TaskService(ITaskRepository repository, ILogger<TaskService> logger)
    : ITaskService
{
    public async Task<IReadOnlyList<Domain.Task>> ListAllAsync(CancellationToken ct = default)
    {
        logger.LogDebug("Listing all tasks");
        return await repository.GetAllAsync(ct);
    }

    public async Task<Result<Domain.Task>> GetByIdAsync(TaskId id, CancellationToken ct = default)
    {
        var task = await repository.GetByIdAsync(id, ct);

        if (task is null)
        {
            logger.LogWarning("Task {TaskId} not found", id);
            return Result<Domain.Task>.Failure(new TaskError.NotFound(id));
        }

        return Result<Domain.Task>.Success(task);
    }

    public async Task<Result<Domain.Task>> CreateAsync(string title, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(title))
            return Result<Domain.Task>.Failure(new TaskError.ValidationError("Title must not be blank."));

        var task = Domain.Task.Create(title);
        await repository.SaveAsync(task, ct);

        logger.LogInformation("Created task {TaskId} with title '{Title}'", task.Id, task.Title);
        return Result<Domain.Task>.Success(task);
    }

    public async Task<Result<bool>> DeleteAsync(TaskId id, CancellationToken ct = default)
    {
        var deleted = await repository.DeleteAsync(id, ct);

        if (!deleted)
        {
            logger.LogWarning("Delete failed — task {TaskId} not found", id);
            return Result<bool>.Failure(new TaskError.NotFound(id));
        }

        logger.LogInformation("Deleted task {TaskId}", id);
        return Result<bool>.Success(true);
    }
}
