using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using DandDTemplateParserCSharp.Controllers;
using DandDTemplateParserCSharp.Domain;
using DandDTemplateParserCSharp.Repositories;
using DomainTask = DandDTemplateParserCSharp.Domain.Task;

namespace DandDTemplateParserCSharp.Tests.Controllers;

// ── Custom factory — replaces Dapper repository with in-memory stub ──────────
public sealed class ApiFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        // Provide a non-empty connection string so ValidateOnStart() passes
        builder.ConfigureAppConfiguration((_, config) =>
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Database:ConnectionString"] = "Server=test-stub;Database=test"
            }));

        builder.ConfigureServices(services =>
        {
            // Swap real repository for in-memory stub
            var descriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(ITaskRepository));
            if (descriptor is not null)
                services.Remove(descriptor);

            services.AddScoped<ITaskRepository, InMemoryTaskRepository>();
        });
    }
}

// ── In-memory repository stub ─────────────────────────────────────────────────
public sealed class InMemoryTaskRepository : ITaskRepository
{
    private readonly List<DomainTask> _store = [];

    public System.Threading.Tasks.Task<IReadOnlyList<DomainTask>> GetAllAsync(CancellationToken ct = default)
        => System.Threading.Tasks.Task.FromResult<IReadOnlyList<DomainTask>>(_store.ToList());

    public System.Threading.Tasks.Task<DomainTask?> GetByIdAsync(TaskId id, CancellationToken ct = default)
        => System.Threading.Tasks.Task.FromResult<DomainTask?>(_store.FirstOrDefault(t => t.Id == id));

    public System.Threading.Tasks.Task<DomainTask> SaveAsync(DomainTask task, CancellationToken ct = default)
    {
        _store.Add(task);
        return System.Threading.Tasks.Task.FromResult(task);
    }

    public System.Threading.Tasks.Task<bool> DeleteAsync(TaskId id, CancellationToken ct = default)
    {
        var task = _store.FirstOrDefault(t => t.Id == id);
        if (task is null) return System.Threading.Tasks.Task.FromResult(false);
        _store.Remove(task);
        return System.Threading.Tasks.Task.FromResult(true);
    }
}

// ── Tests ────────────────────────────────────────────────────────────────────
public sealed class TasksControllerIntegrationTests(ApiFactory factory)
    : IClassFixture<ApiFactory>
{
    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async System.Threading.Tasks.Task Post_WithValidTitle_Returns201AndCreatedTask()
    {
        var response = await _client.PostAsJsonAsync("/api/v1/tasks", new CreateTaskRequest("Buy milk"));

        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var task = await response.Content.ReadFromJsonAsync<DomainTask>();
        task!.Title.Should().Be("Buy milk");
    }

    [Fact]
    public async System.Threading.Tasks.Task Post_WithEmptyTitle_Returns400()
    {
        var response = await _client.PostAsJsonAsync("/api/v1/tasks", new CreateTaskRequest(""));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async System.Threading.Tasks.Task Get_UnknownId_Returns404()
    {
        var response = await _client.GetAsync($"/api/v1/tasks/{Guid.NewGuid()}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}
