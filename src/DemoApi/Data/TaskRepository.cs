using DemoApi.Domain;
using Npgsql;
using NpgsqlTypes;

namespace DemoApi.Data;

public interface ITaskRepository
{
    Task<IReadOnlyList<TaskItem>> ListAsync(int? projectId, TaskItemStatus? status, CancellationToken ct);
    Task<TaskItem?> GetAsync(int id, CancellationToken ct);
    Task<TaskItem> CreateAsync(int projectId, string title, string? description, DateOnly? dueDate, CancellationToken ct);
    Task<TaskItem?> UpdateAsync(int id, string title, string? description, DateOnly? dueDate, CancellationToken ct);
    Task<TaskItem?> SetStatusAsync(int id, TaskItemStatus status, CancellationToken ct);
    Task<bool> DeleteAsync(int id, CancellationToken ct);
}

public sealed class TaskRepository(NpgsqlDataSource db) : ITaskRepository
{
    private const string Columns = "id, project_id, title, description, status, due_date, created_at, updated_at";

    public async Task<IReadOnlyList<TaskItem>> ListAsync(int? projectId, TaskItemStatus? status, CancellationToken ct)
    {
        await using var cmd = db.CreateCommand($"""
            SELECT {Columns} FROM tasks
            WHERE ($1 IS NULL OR project_id = $1)
              AND ($2 IS NULL OR status = $2)
            ORDER BY id
            """);
        cmd.Parameters.Add(Sql.Param(projectId, NpgsqlDbType.Integer));
        cmd.Parameters.Add(Sql.Param(status is null ? null : Sql.ToDb(status.Value), NpgsqlDbType.Text));
        return await ReadAllAsync(cmd, ct);
    }

    public async Task<TaskItem?> GetAsync(int id, CancellationToken ct)
    {
        await using var cmd = db.CreateCommand($"SELECT {Columns} FROM tasks WHERE id = $1");
        cmd.Parameters.Add(Sql.Param(id, NpgsqlDbType.Integer));
        return (await ReadAllAsync(cmd, ct)).FirstOrDefault();
    }

    public async Task<TaskItem> CreateAsync(int projectId, string title, string? description, DateOnly? dueDate, CancellationToken ct)
    {
        await using var cmd = db.CreateCommand($"""
            INSERT INTO tasks (project_id, title, description, due_date)
            VALUES ($1, $2, $3, $4)
            RETURNING {Columns}
            """);
        cmd.Parameters.Add(Sql.Param(projectId, NpgsqlDbType.Integer));
        cmd.Parameters.Add(Sql.Param(title, NpgsqlDbType.Text));
        cmd.Parameters.Add(Sql.Param(description, NpgsqlDbType.Text));
        cmd.Parameters.Add(Sql.Param(dueDate, NpgsqlDbType.Date));
        return (await ReadAllAsync(cmd, ct))[0];
    }

    public async Task<TaskItem?> UpdateAsync(int id, string title, string? description, DateOnly? dueDate, CancellationToken ct)
    {
        await using var cmd = db.CreateCommand($"""
            UPDATE tasks SET title = $2, description = $3, due_date = $4, updated_at = now()
            WHERE id = $1
            RETURNING {Columns}
            """);
        cmd.Parameters.Add(Sql.Param(id, NpgsqlDbType.Integer));
        cmd.Parameters.Add(Sql.Param(title, NpgsqlDbType.Text));
        cmd.Parameters.Add(Sql.Param(description, NpgsqlDbType.Text));
        cmd.Parameters.Add(Sql.Param(dueDate, NpgsqlDbType.Date));
        return (await ReadAllAsync(cmd, ct)).FirstOrDefault();
    }

    public async Task<TaskItem?> SetStatusAsync(int id, TaskItemStatus status, CancellationToken ct)
    {
        await using var cmd = db.CreateCommand($"""
            UPDATE tasks SET status = $2, updated_at = now()
            WHERE id = $1
            RETURNING {Columns}
            """);
        cmd.Parameters.Add(Sql.Param(id, NpgsqlDbType.Integer));
        cmd.Parameters.Add(Sql.Param(Sql.ToDb(status), NpgsqlDbType.Text));
        return (await ReadAllAsync(cmd, ct)).FirstOrDefault();
    }

    public async Task<bool> DeleteAsync(int id, CancellationToken ct)
    {
        await using var cmd = db.CreateCommand("DELETE FROM tasks WHERE id = $1");
        cmd.Parameters.Add(Sql.Param(id, NpgsqlDbType.Integer));
        return await cmd.ExecuteNonQueryAsync(ct) > 0;
    }

    private static async Task<List<TaskItem>> ReadAllAsync(NpgsqlCommand cmd, CancellationToken ct)
    {
        await using var reader = await cmd.ExecuteReaderAsync(ct);
        var result = new List<TaskItem>();
        while (await reader.ReadAsync(ct))
        {
            result.Add(new TaskItem(
                reader.GetInt32(0),
                reader.GetInt32(1),
                reader.GetString(2),
                reader.IsDBNull(3) ? null : reader.GetString(3),
                Sql.StatusFromDb(reader.GetString(4)),
                reader.IsDBNull(5) ? null : reader.GetFieldValue<DateOnly>(5),
                reader.GetFieldValue<DateTimeOffset>(6),
                reader.GetFieldValue<DateTimeOffset>(7)));
        }
        return result;
    }
}
