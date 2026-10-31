using DemoApi.Data;
using DemoApi.Domain;

namespace DemoApi.Services;

// Действия с проектами. null / false в ответе — «проекта нет»: что ответить клиенту, решает контроллер
public sealed class ProjectService(IProjectRepository projects, ILogger<ProjectService> log)
{
    public Task<IReadOnlyList<Project>> ListAsync(CancellationToken ct) => projects.ListAsync(ct);

    public Task<Project?> GetAsync(int id, CancellationToken ct) => projects.GetAsync(id, ct);

    public Task<bool> NameExistsAsync(string name, CancellationToken ct) => projects.NameExistsAsync(name, ct);

    public async Task<Project> CreateAsync(string name, CancellationToken ct)
    {
        var project = await projects.CreateAsync(name, ct);
        log.LogInformation("Создан проект {ProjectId} «{ProjectName}»", project.Id, project.Name);
        return project;
    }

    public async Task<Project?> RenameAsync(int id, string name, CancellationToken ct)
    {
        var project = await projects.RenameAsync(id, name, ct);
        if (project is not null)
        {
            log.LogInformation("Проект {ProjectId} переименован в «{ProjectName}»", id, name);
        }
        return project;
    }

    public async Task<bool> DeleteAsync(int id, CancellationToken ct)
    {
        var deleted = await projects.DeleteAsync(id, ct);
        if (deleted)
        {
            log.LogInformation("Удалён проект {ProjectId}", id);
        }
        return deleted;
    }

    public Task<ProjectStats> GetStatsAsync(int id, CancellationToken ct) => projects.GetStatsAsync(id, ct);
}
