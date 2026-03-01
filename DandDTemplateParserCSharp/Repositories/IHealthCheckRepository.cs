namespace DandDTemplateParserCSharp.Repositories;

public interface IHealthCheckRepository
{
    Task PingAsync(CancellationToken ct = default);
}
