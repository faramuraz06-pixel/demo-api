namespace DemoApi.Domain;

/// <summary>Задача</summary>
/// <param name="Id">Идентификатор</param>
/// <param name="ProjectId">Проект, к которому относится задача</param>
/// <param name="Title">Название</param>
/// <param name="Description">Описание</param>
/// <param name="Status">Статус: Todo, InProgress, Done</param>
/// <param name="DueDate">Срок</param>
/// <param name="CreatedAt">Когда создана</param>
/// <param name="UpdatedAt">Когда последний раз изменена</param>
public sealed record TaskItem(
    int Id,
    int ProjectId,
    string Title,
    string? Description,
    TaskItemStatus Status,
    DateOnly? DueDate,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);
