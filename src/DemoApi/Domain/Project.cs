namespace DemoApi.Domain;

/// <summary>Проект</summary>
/// <param name="Id">Идентификатор</param>
/// <param name="Name">Название, уникальное</param>
/// <param name="CreatedAt">Когда создан</param>
public sealed record Project(int Id, string Name, DateTimeOffset CreatedAt);
