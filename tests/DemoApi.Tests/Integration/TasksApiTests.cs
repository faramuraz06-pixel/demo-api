using System.Net;
using System.Net.Http.Json;
using DemoApi.Domain;
using Microsoft.AspNetCore.Mvc.Testing;

namespace DemoApi.Tests.Integration;

[Collection(ApiCollection.Name)]
public class TasksApiTests(WebApplicationFactory<Program> factory)
{
    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task Create_InExistingProject_ReturnsCreatedTodoTask()
    {
        var project = await _client.CreateProjectAsync();

        var response = await _client.PostAsJsonAsync("/api/tasks", new { projectId = project.Id, title = "Написать Dockerfile" });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var task = await response.Content.ReadFromJsonAsync<TaskItem>(TestApi.Json);
        Assert.Equal(project.Id, task!.ProjectId);
        Assert.Equal(TaskItemStatus.Todo, task.Status);
    }

    [Fact]
    public async Task Create_InUnknownProject_ReturnsNotFound()
    {
        var response = await _client.PostAsJsonAsync("/api/tasks", new { projectId = int.MaxValue, title = "Задача" });

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Create_EmptyTitle_ReturnsBadRequest()
    {
        var project = await _client.CreateProjectAsync();

        var response = await _client.PostAsJsonAsync("/api/tasks", new { projectId = project.Id, title = "" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task ChangeStatus_TodoToDone_ReturnsConflict()
    {
        var task = await _client.CreateTaskAsync((await _client.CreateProjectAsync()).Id);

        var response = await _client.ChangeStatusAsync(task.Id, TaskItemStatus.Done);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task ChangeStatus_TodoInProgressDone_Succeeds()
    {
        var task = await _client.CreateTaskAsync((await _client.CreateProjectAsync()).Id);

        (await _client.ChangeStatusAsync(task.Id, TaskItemStatus.InProgress)).EnsureSuccessStatusCode();
        var response = await _client.ChangeStatusAsync(task.Id, TaskItemStatus.Done);

        response.EnsureSuccessStatusCode();
        var done = await response.Content.ReadFromJsonAsync<TaskItem>(TestApi.Json);
        Assert.Equal(TaskItemStatus.Done, done!.Status);
    }

    [Fact]
    public async Task List_FiltersByProjectAndStatus()
    {
        var project = await _client.CreateProjectAsync();
        var first = await _client.CreateTaskAsync(project.Id, "Первая");
        await _client.CreateTaskAsync(project.Id, "Вторая");
        (await _client.ChangeStatusAsync(first.Id, TaskItemStatus.InProgress)).EnsureSuccessStatusCode();

        var inProgress = await _client.GetFromJsonAsync<List<TaskItem>>(
            $"/api/tasks?projectId={project.Id}&status=InProgress", TestApi.Json);

        var single = Assert.Single(inProgress!);
        Assert.Equal(first.Id, single.Id);
    }

    [Fact]
    public async Task Delete_ThenGet_ReturnsNotFound()
    {
        var task = await _client.CreateTaskAsync((await _client.CreateProjectAsync()).Id);

        var delete = await _client.DeleteAsync($"/api/tasks/{task.Id}");
        var get = await _client.GetAsync($"/api/tasks/{task.Id}");

        Assert.Equal(HttpStatusCode.NoContent, delete.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, get.StatusCode);
    }
}
