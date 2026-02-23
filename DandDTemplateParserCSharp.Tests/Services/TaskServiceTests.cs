using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using NSubstitute.ReturnsExtensions;
using Xunit;
using DandDTemplateParserCSharp.Domain;
using DandDTemplateParserCSharp.Repositories;
using DandDTemplateParserCSharp.Services;
using DomainTask = DandDTemplateParserCSharp.Domain.Task;

namespace DandDTemplateParserCSharp.Tests.Services;

public sealed class TaskServiceTests
{
    private readonly ITaskRepository _repository = Substitute.For<ITaskRepository>();
    private readonly TaskService     _sut;

    public TaskServiceTests()
    {
        _sut = new TaskService(_repository, NullLogger<TaskService>.Instance);
    }

    // ── ListAllAsync ────────────────────────────────────────────────────────
    [Fact]
    public async System.Threading.Tasks.Task ListAllAsync_WhenCalled_ReturnsRepositoryResult()
    {
        var tasks = new[] { DomainTask.Create("Buy milk") };
        _repository.GetAllAsync().Returns(tasks);

        var result = await _sut.ListAllAsync();

        result.Should().BeEquivalentTo(tasks);
    }

    // ── GetByIdAsync ────────────────────────────────────────────────────────
    [Fact]
    public async System.Threading.Tasks.Task GetByIdAsync_WhenTaskExists_ReturnsSuccess()
    {
        var task = DomainTask.Create("Buy milk");
        _repository.GetByIdAsync(task.Id).Returns(task);

        var result = await _sut.GetByIdAsync(task.Id);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(task);
    }

    [Fact]
    public async System.Threading.Tasks.Task GetByIdAsync_WhenTaskMissing_ReturnsNotFoundError()
    {
        var id = TaskId.New();
        _repository.GetByIdAsync(id).ReturnsNull();

        var result = await _sut.GetByIdAsync(id);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().BeOfType<TaskError.NotFound>();
    }

    // ── CreateAsync ─────────────────────────────────────────────────────────
    [Fact]
    public async System.Threading.Tasks.Task CreateAsync_WithValidTitle_ReturnsCreatedTask()
    {
        _repository.SaveAsync(Arg.Any<DomainTask>())
                   .Returns(callInfo => callInfo.Arg<DomainTask>());

        var result = await _sut.CreateAsync("Walk the dog");

        result.IsSuccess.Should().BeTrue();
        result.Value.Title.Should().Be("Walk the dog");
        await _repository.Received(1).SaveAsync(Arg.Is<DomainTask>(t => t.Title == "Walk the dog"));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public async System.Threading.Tasks.Task CreateAsync_WithBlankTitle_ReturnsValidationError(string? title)
    {
        var result = await _sut.CreateAsync(title!);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().BeOfType<TaskError.ValidationError>();
        await _repository.DidNotReceive().SaveAsync(Arg.Any<DomainTask>());
    }

    // ── DeleteAsync ─────────────────────────────────────────────────────────
    [Fact]
    public async System.Threading.Tasks.Task DeleteAsync_WhenTaskExists_ReturnsSuccess()
    {
        var id = TaskId.New();
        _repository.DeleteAsync(id).Returns(true);

        var result = await _sut.DeleteAsync(id);

        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async System.Threading.Tasks.Task DeleteAsync_WhenTaskMissing_ReturnsNotFoundError()
    {
        var id = TaskId.New();
        _repository.DeleteAsync(id).Returns(false);

        var result = await _sut.DeleteAsync(id);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().BeOfType<TaskError.NotFound>();
    }
}
