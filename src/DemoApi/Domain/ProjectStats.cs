namespace DemoApi.Domain;

/// <summary>Статистика задач проекта</summary>
/// <param name="ProjectId">Идентификатор проекта</param>
/// <param name="Total">Всего задач</param>
/// <param name="Todo">Задач в статусе Todo</param>
/// <param name="InProgress">Задач в статусе InProgress</param>
/// <param name="Done">Задач в статусе Done</param>
/// <param name="Overdue">Просроченных задач (срок раньше сегодня и статус не Done)</param>
public sealed record ProjectStats(
    int ProjectId,
    int Total,
    int Todo,
    int InProgress,
    int Done,
    int Overdue);