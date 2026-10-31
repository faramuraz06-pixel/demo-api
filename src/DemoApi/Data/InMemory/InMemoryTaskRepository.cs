using DemoApi.Domain;

namespace DemoApi.Data.InMemory;

public sealed class InMemoryTaskRepository(InMemoryStore store) : ITaskRepository
{
    public Task<IReadOnlyList<TaskItem>> ListAsync(int? projectId, TaskItemStatus? status, CancellationToken ct)
    {
        lock (store.Sync)
        {
            IReadOnlyList<TaskItem> result = store.Tasks
                .Where(t => projectId is null || t.ProjectId == projectId)
                .Where(t => status is null || t.Status == status)
                .OrderBy(t => t.Id)
                .ToList();
            return Task.FromResult(result);
        }
    }

    public Task<TaskItem?> GetAsync(int id, CancellationToken ct)
    {
        lock (store.Sync)
        {
            return Task.FromResult(store.Tasks.FirstOrDefault(t => t.Id == id));
        }
    }

    public Task<TaskItem> CreateAsync(int projectId, string title, string? description, DateOnly? dueDate, CancellationToken ct)
    {
        lock (store.Sync)
        {
            var now = DateTimeOffset.UtcNow;
            var task = new TaskItem(store.NextTaskId++, projectId, title, description, TaskItemStatus.Todo, dueDate, now, now);
            store.Tasks.Add(task);
            return Task.FromResult(task);
        }
    }

    public Task<TaskItem?> UpdateAsync(int id, string title, string? description, DateOnly? dueDate, CancellationToken ct) =>
        Replace(id, t => t with { Title = title, Description = description, DueDate = dueDate, UpdatedAt = DateTimeOffset.UtcNow });

    public Task<TaskItem?> SetStatusAsync(int id, TaskItemStatus status, CancellationToken ct) =>
        Replace(id, t => t with { Status = status, UpdatedAt = DateTimeOffset.UtcNow });

    public Task<bool> DeleteAsync(int id, CancellationToken ct)
    {
        lock (store.Sync)
        {
            return Task.FromResult(store.Tasks.RemoveAll(t => t.Id == id) > 0);
        }
    }

    // Задачи — неизменяемые record: заменяем задачу обновлённой копией
    private Task<TaskItem?> Replace(int id, Func<TaskItem, TaskItem> change)
    {
        lock (store.Sync)
        {
            var index = store.Tasks.FindIndex(t => t.Id == id);
            if (index < 0)
            {
                return Task.FromResult<TaskItem?>(null);
            }
            store.Tasks[index] = change(store.Tasks[index]);
            return Task.FromResult<TaskItem?>(store.Tasks[index]);
        }
    }
}
