using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using DemoApi.Domain;

namespace DemoApi.Tests.Integration;

internal static class TestApi
{
    public static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() },
    };

    // БД между запусками не чистится — имена делаем уникальными
    public static string Unique(string prefix) => $"{prefix}-{Guid.NewGuid():N}";

    public static async Task<Project> CreateProjectAsync(this HttpClient client)
    {
        var response = await client.PostAsJsonAsync("/api/projects", new { name = Unique("project") });
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<Project>(Json))!;
    }

    public static async Task<TaskItem> CreateTaskAsync(this HttpClient client, int projectId, string title = "Задача")
    {
        var response = await client.PostAsJsonAsync("/api/tasks", new { projectId, title });
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<TaskItem>(Json))!;
    }

    public static Task<HttpResponseMessage> ChangeStatusAsync(this HttpClient client, int taskId, TaskItemStatus status) =>
        client.PatchAsJsonAsync($"/api/tasks/{taskId}/status", new { status }, Json);
}
