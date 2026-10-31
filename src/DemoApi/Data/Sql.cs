using DemoApi.Domain;
using Npgsql;
using NpgsqlTypes;

namespace DemoApi.Data;

internal static class Sql
{
    // Позиционный параметр ($1, $2, ...) с явным типом — чтобы NULL не ломал вывод типов в PostgreSQL
    public static NpgsqlParameter Param(object? value, NpgsqlDbType type) =>
        new() { Value = value ?? DBNull.Value, NpgsqlDbType = type };

    public static string ToDb(TaskItemStatus status) => status switch
    {
        TaskItemStatus.Todo => "todo",
        TaskItemStatus.InProgress => "in_progress",
        TaskItemStatus.Done => "done",
        _ => throw new ArgumentOutOfRangeException(nameof(status), status, null),
    };

    public static TaskItemStatus StatusFromDb(string value) => value switch
    {
        "todo" => TaskItemStatus.Todo,
        "in_progress" => TaskItemStatus.InProgress,
        "done" => TaskItemStatus.Done,
        _ => throw new InvalidOperationException($"Неизвестный статус в БД: {value}"),
    };
}
