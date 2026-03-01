using Microsoft.Extensions.Diagnostics.HealthChecks;
using DandDTemplateParserCSharp.Repositories;

namespace DandDTemplateParserCSharp.HealthChecks;

public sealed class SqlServerHealthCheck(IHealthCheckRepository repo) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            await repo.PingAsync(cancellationToken);
            return HealthCheckResult.Healthy();
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy("SQL Server is unreachable.", ex);
        }
    }
}
