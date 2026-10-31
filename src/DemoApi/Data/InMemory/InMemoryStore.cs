using DemoApi.Domain;

namespace DemoApi.Data.InMemory;

// Хранилище в памяти процесса: данные живут, пока работает сервис.
// Нужно, чтобы запустить проект без БД (темы 1–2). С темы 3 — PostgreSQL.
public sealed class InMemoryStore
{
    public object Sync { get; } = new();
    public List<Project> Projects { get; } = [];
    public List<TaskItem> Tasks { get; } = [];
    public int NextProjectId { get; set; } = 1;
    public int NextTaskId { get; set; } = 1;
}
