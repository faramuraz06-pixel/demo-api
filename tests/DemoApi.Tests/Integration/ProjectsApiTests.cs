using System.Net;
using System.Net.Http.Json;
using DemoApi.Domain;
using Microsoft.AspNetCore.Mvc.Testing;

namespace DemoApi.Tests.Integration;

[Collection(ApiCollection.Name)]
public class ProjectsApiTests(WebApplicationFactory<Program> factory)
{
    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task Create_ReturnsCreatedWithLocation()
    {
        var response = await _client.PostAsJsonAsync("/api/projects", new { name = TestApi.Unique("project") });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.NotNull(response.Headers.Location);
    }

    [Fact]
    public async Task Create_DuplicateName_ReturnsConflict()
    {
        var name = TestApi.Unique("duplicate");
        (await _client.PostAsJsonAsync("/api/projects", new { name })).EnsureSuccessStatusCode();

        var response = await _client.PostAsJsonAsync("/api/projects", new { name });

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task Create_EmptyName_ReturnsBadRequest()
    {
        var response = await _client.PostAsJsonAsync("/api/projects", new { name = "" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Rename_ChangesName()
    {
        var project = await _client.CreateProjectAsync();
        var newName = TestApi.Unique("renamed");

        var response = await _client.PutAsJsonAsync($"/api/projects/{project.Id}", new { name = newName });

        response.EnsureSuccessStatusCode();
        var renamed = await response.Content.ReadFromJsonAsync<Project>(TestApi.Json);
        Assert.Equal(newName, renamed!.Name);
    }

    [Fact]
    public async Task Rename_ToExistingName_ReturnsConflict()
    {
        var first = await _client.CreateProjectAsync();
        var second = await _client.CreateProjectAsync();

        var response = await _client.PutAsJsonAsync($"/api/projects/{second.Id}", new { name = first.Name });

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task Get_Unknown_ReturnsNotFound()
    {
        var response = await _client.GetAsync($"/api/projects/{int.MaxValue}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Stats_CountsByStatusAndOverdue()
    {
        var project = await _client.CreateProjectAsync();
        var started = await _client.CreateTaskAsync(project.Id, "В работе");
        await _client.CreateTaskAsync(project.Id, "Новая");
        var overdue = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-2));
        (await _client.PostAsJsonAsync("/api/tasks", new { projectId = project.Id, title = "Просрочена", dueDate = overdue }))
            .EnsureSuccessStatusCode();
        (await _client.ChangeStatusAsync(started.Id, TaskItemStatus.InProgress)).EnsureSuccessStatusCode();

        var stats = await _client.GetFromJsonAsync<ProjectStats>($"/api/projects/{project.Id}/stats", TestApi.Json);

        Assert.Equal(new ProjectStats(project.Id, Total: 3, Todo: 2, InProgress: 1, Done: 0, Overdue: 1), stats);
    }

    [Fact]
    public async Task Stats_UnknownProject_ReturnsNotFound()
    {
        var response = await _client.GetAsync($"/api/projects/{int.MaxValue}/stats");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}
