using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Options;
using DandDTemplateParserCSharp.Domain;
using DandDTemplateParserCSharp.Options;

namespace DandDTemplateParserCSharp.Repositories;

public sealed class DapperTaskRepository(IOptions<DatabaseOptions> dbOptions, ILogger<DapperTaskRepository> logger)
    : ITaskRepository
{
    private readonly string _connectionString = dbOptions.Value.ConnectionString;

    public async Task<IReadOnlyList<Domain.Task>> GetAllAsync(CancellationToken ct = default)
    {
        const string sql = "SELECT Id, Title, CreatedAt FROM Tasks ORDER BY CreatedAt DESC";
        await using var conn = new SqlConnection(_connectionString);

        logger.LogDebug("Fetching all tasks");
        var rows = await conn.QueryAsync<TaskRow>(new CommandDefinition(sql, cancellationToken: ct));
        return rows.Select(Map).ToList();
    }

    public async Task<Domain.Task?> GetByIdAsync(TaskId id, CancellationToken ct = default)
    {
        const string sql = "SELECT Id, Title, CreatedAt FROM Tasks WHERE Id = @Id";
        await using var conn = new SqlConnection(_connectionString);

        var row = await conn.QuerySingleOrDefaultAsync<TaskRow>(
            new CommandDefinition(sql, new { Id = id.Value }, cancellationToken: ct));

        return row is null ? null : Map(row);
    }

    public async Task<Domain.Task> SaveAsync(Domain.Task task, CancellationToken ct = default)
    {
        const string sql = """
            INSERT INTO Tasks (Id, Title, CreatedAt)
            VALUES (@Id, @Title, @CreatedAt)
            """;
        await using var conn = new SqlConnection(_connectionString);

        logger.LogDebug("Saving task {TaskId}", task.Id);
        await conn.ExecuteAsync(new CommandDefinition(
            sql,
            new { Id = task.Id.Value, task.Title, task.CreatedAt },
            cancellationToken: ct));

        return task;
    }

    public async Task<bool> DeleteAsync(TaskId id, CancellationToken ct = default)
    {
        const string sql = "DELETE FROM Tasks WHERE Id = @Id";
        await using var conn = new SqlConnection(_connectionString);

        var affected = await conn.ExecuteAsync(
            new CommandDefinition(sql, new { Id = id.Value }, cancellationToken: ct));

        return affected > 0;
    }

    private static Domain.Task Map(TaskRow r) =>
        Domain.Task.Reconstitute(TaskId.From(r.Id), r.Title, r.CreatedAt);

    // Private DTO — maps to DB columns, never exposed outside this class
    private sealed record TaskRow(Guid Id, string Title, DateTime CreatedAt);
}
