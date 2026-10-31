using DemoApi.Domain;

namespace DemoApi.Data.InMemory;

public sealed class InMemoryProjectRepository(InMemoryStore store) : IProjectRepository
{
    public Task<IReadOnlyList<Project>> ListAsync(CancellationToken ct)
    {
        lock (store.Sync)
        {
            IReadOnlyList<Project> result = store.Projects.OrderBy(p => p.Id).ToList();
            return Task.FromResult(result);
        }
    }

    public Task<Project?> GetAsync(int id, CancellationToken ct)
    {
        lock (store.Sync)
        {
            return Task.FromResult(store.Projects.FirstOrDefault(p => p.Id == id));
        }
    }

    public Task<bool> NameExistsAsync(string name, CancellationToken ct)
    {
        lock (store.Sync)
        {
            return Task.FromResult(store.Projects.Any(p => p.Name == name));
        }
    }

    public Task<Project> CreateAsync(string name, CancellationToken ct)
    {
        lock (store.Sync)
        {
            var project = new Project(store.NextProjectId++, name, DateTimeOffset.UtcNow);
            store.Projects.Add(project);
            return Task.FromResult(project);
        }
    }

    public Task<Project?> RenameAsync(int id, string name, CancellationToken ct)
    {
        lock (store.Sync)
        {
            var index = store.Projects.FindIndex(p => p.Id == id);
            if (index < 0)
            {
                return Task.FromResult<Project?>(null);
            }
            store.Projects[index] = store.Projects[index] with { Name = name };
            return Task.FromResult<Project?>(store.Projects[index]);
        }
    }

    public Task<bool> DeleteAsync(int id, CancellationToken ct)
    {
        lock (store.Sync)
        {
            store.Tasks.RemoveAll(t => t.ProjectId == id);   // как ON DELETE CASCADE в БД
            return Task.FromResult(store.Projects.RemoveAll(p => p.Id == id) > 0);
        }
    }

    public Task<ProjectStats> GetStatsAsync(int projectId, CancellationToken ct)
    {
        lock (store.Sync)
        {
            var today = DateOnly.FromDateTime(DateTime.UtcNow);
            var tasks = store.Tasks.Where(t => t.ProjectId == projectId).ToList();
            return Task.FromResult(new ProjectStats(
                projectId,
                tasks.Count,
                tasks.Count(t => t.Status == TaskItemStatus.Todo),
                tasks.Count(t => t.Status == TaskItemStatus.InProgress),
                tasks.Count(t => t.Status == TaskItemStatus.Done),
                tasks.Count(t => t.DueDate < today && t.Status != TaskItemStatus.Done)));
        }
    }
}
