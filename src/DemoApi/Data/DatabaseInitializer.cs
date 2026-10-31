using Npgsql;

namespace DemoApi.Data;

// Схема БД создаётся при старте. Advisory lock не даёт двум экземплярам API создавать таблицы одновременно.
public static class DatabaseInitializer
{
    private const string Schema = """
        SELECT pg_advisory_xact_lock(4242);

        CREATE TABLE IF NOT EXISTS projects (
            id          serial PRIMARY KEY,
            name        text        NOT NULL UNIQUE,
            created_at  timestamptz NOT NULL DEFAULT now()
        );

        CREATE TABLE IF NOT EXISTS tasks (
            id          serial PRIMARY KEY,
            project_id  int         NOT NULL REFERENCES projects (id) ON DELETE CASCADE,
            title       text        NOT NULL,
            description text        NULL,
            status      text        NOT NULL DEFAULT 'todo' CHECK (status IN ('todo', 'in_progress', 'done')),
            due_date    date        NULL,
            created_at  timestamptz NOT NULL DEFAULT now(),
            updated_at  timestamptz NOT NULL DEFAULT now()
        );

        CREATE INDEX IF NOT EXISTS ix_tasks_project_status ON tasks (project_id, status);
        """;

    public static async Task EnsureSchemaAsync(NpgsqlDataSource db, CancellationToken ct = default)
    {
        await using var connection = await db.OpenConnectionAsync(ct);
        await using var tx = await connection.BeginTransactionAsync(ct);
        await using (var cmd = new NpgsqlCommand(Schema, connection, tx))
        {
            await cmd.ExecuteNonQueryAsync(ct);
        }
        await tx.CommitAsync(ct);
    }
}
