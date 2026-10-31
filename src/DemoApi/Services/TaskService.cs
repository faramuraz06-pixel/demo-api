using DemoApi.Data;
using DemoApi.Domain;

namespace DemoApi.Services;

// Действия с задачами.null / false в ответе — «задачи (или проекта) нет»: что ответить клиенту, решает контроллер
public sealed class TaskService(ITaskRepository tasks, IProjectRepository projects, ILogger<TaskService> log)
{
    public Task<IReadOnlyList<TaskItem>> ListAsync(int? projectId, TaskItemStatus? status, CancellationToken ct) =>
        tasks.ListAsync(projectId, status, ct);

    public Task<TaskItem?> GetAsync(int id, CancellationToken ct) => tasks.GetAsync(id, ct);

    // null — проекта с таким id нет
    public async Task<TaskItem?> CreateAsync(int projectId, string title, string? description, DateOnly? dueDate, CancellationToken ct)
    {
        if (await projects.GetAsync(projectId, ct) is null)
        {
            return null;
        }

        var task = await tasks.CreateAsync(projectId, title, description, dueDate, ct);
        log.LogInformation("Создана задача {TaskId} в проекте {ProjectId}", task.Id, task.ProjectId);
        return task;
    }

    public Task<TaskItem?> UpdateAsync(int id, string title, string? description, DateOnly? dueDate, CancellationToken ct) =>
        tasks.UpdateAsync(id, title, description, dueDate, ct);

    public async Task<TaskItem?> ChangeStatusAsync(TaskItem task, TaskItemStatus status, CancellationToken ct)
    {
        var updated = await tasks.SetStatusAsync(task.Id, status, ct);
        log.LogInformation("Задача {TaskId}: {OldStatus} -> {NewStatus}", task.Id, task.Status, status);
        return updated;
    }

    public Task<bool> DeleteAsync(int id, CancellationToken ct) => tasks.DeleteAsync(id, ct);
}
