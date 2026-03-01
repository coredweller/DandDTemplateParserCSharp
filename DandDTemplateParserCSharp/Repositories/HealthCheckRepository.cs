using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Options;
using DandDTemplateParserCSharp.Options;

namespace DandDTemplateParserCSharp.Repositories;

public sealed class HealthCheckRepository(
    IOptions<DatabaseOptions> dbOptions,
    ILogger<HealthCheckRepository> logger)
    : IHealthCheckRepository
{
    private readonly string _connectionString = dbOptions.Value.ConnectionString;

    public async Task PingAsync(CancellationToken ct = default)
    {
        logger.LogDebug("Pinging SQL Server");
        await using var conn = new SqlConnection(_connectionString);
        await conn.OpenAsync(ct);
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT 1";
        await cmd.ExecuteScalarAsync(ct);
    }
}
