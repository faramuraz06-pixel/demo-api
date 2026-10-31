using DemoApi.Domain;
using Npgsql;
using NpgsqlTypes;

namespace DemoApi.Data;

public interface IProjectRepository
{
    Task<IReadOnlyList<Project>> ListAsync(CancellationToken ct);
    Task<Project?> GetAsync(int id, CancellationToken ct);
    Task<bool> NameExistsAsync(string name, CancellationToken ct);
    Task<Project> CreateAsync(string name, CancellationToken ct);
    Task<Project?> RenameAsync(int id, string name, CancellationToken ct);
    Task<bool> DeleteAsync(int id, CancellationToken ct);
    Task<ProjectStats> GetStatsAsync(int projectId, CancellationToken ct);
}

public sealed class ProjectRepository(NpgsqlDataSource db) : IProjectRepository
{
    private const string Columns = "id, name, created_at";

    public async Task<IReadOnlyList<Project>> ListAsync(CancellationToken ct)
    {
        await using var cmd = db.CreateCommand($"SELECT {Columns} FROM projects ORDER BY id");
        await using var reader = await cmd.ExecuteReaderAsync(ct);
        var result = new List<Project>();
        while (await reader.ReadAsync(ct))
        {
            result.Add(Map(reader));
        }
        return result;
    }

    public async Task<Project?> GetAsync(int id, CancellationToken ct)
    {
        await using var cmd = db.CreateCommand($"SELECT {Columns} FROM projects WHERE id = $1");
        cmd.Parameters.Add(Sql.Param(id, NpgsqlDbType.Integer));
        await using var reader = await cmd.ExecuteReaderAsync(ct);
        return await reader.ReadAsync(ct) ? Map(reader) : null;
    }

    public async Task<bool> NameExistsAsync(string name, CancellationToken ct)
    {
        await using var cmd = db.CreateCommand("SELECT EXISTS (SELECT 1 FROM projects WHERE name = $1)");
        cmd.Parameters.Add(Sql.Param(name, NpgsqlDbType.Text));
        return (bool)(await cmd.ExecuteScalarAsync(ct))!;
    }

    public async Task<Project> CreateAsync(string name, CancellationToken ct)
    {
        await using var cmd = db.CreateCommand($"INSERT INTO projects (name) VALUES ($1) RETURNING {Columns}");
        cmd.Parameters.Add(Sql.Param(name, NpgsqlDbType.Text));
        await using var reader = await cmd.ExecuteReaderAsync(ct);
        await reader.ReadAsync(ct);
        return Map(reader);
    }

    public async Task<Project?> RenameAsync(int id, string name, CancellationToken ct)
    {
        await using var cmd = db.CreateCommand($"UPDATE projects SET name = $2 WHERE id = $1 RETURNING {Columns}");
        cmd.Parameters.Add(Sql.Param(id, NpgsqlDbType.Integer));
        cmd.Parameters.Add(Sql.Param(name, NpgsqlDbType.Text));
        await using var reader = await cmd.ExecuteReaderAsync(ct);
        return await reader.ReadAsync(ct) ? Map(reader) : null;
    }

    public async Task<bool> DeleteAsync(int id, CancellationToken ct)
    {
        await using var cmd = db.CreateCommand("DELETE FROM projects WHERE id = $1");
        cmd.Parameters.Add(Sql.Param(id, NpgsqlDbType.Integer));
        return await cmd.ExecuteNonQueryAsync(ct) > 0;
    }

    public async Task<ProjectStats> GetStatsAsync(int projectId, CancellationToken ct)
    {
        await using var cmd = db.CreateCommand("""
            SELECT count(*),
                   count(*) FILTER (WHERE status = 'todo'),
                   count(*) FILTER (WHERE status = 'in_progress'),
                   count(*) FILTER (WHERE status = 'done'),
                   count(*) FILTER (WHERE due_date < current_date AND status <> 'done')
            FROM tasks
            WHERE project_id = $1
            """);
        cmd.Parameters.Add(Sql.Param(projectId, NpgsqlDbType.Integer));
        await using var reader = await cmd.ExecuteReaderAsync(ct);
        await reader.ReadAsync(ct);
        return new ProjectStats(projectId,
            (int)reader.GetInt64(0), (int)reader.GetInt64(1), (int)reader.GetInt64(2),
            (int)reader.GetInt64(3), (int)reader.GetInt64(4));
    }

    private static Project Map(NpgsqlDataReader r) =>
        new(r.GetInt32(0), r.GetString(1), r.GetFieldValue<DateTimeOffset>(2));
}

