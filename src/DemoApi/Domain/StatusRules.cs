namespace DemoApi.Domain;

// Разрешённые переходы статусов задачи: Todo -> InProgress -> Done, с возвратом на шаг назад
public static class StatusRules
{
    private static readonly Dictionary<TaskItemStatus, TaskItemStatus[]> Allowed = new()
    {
        [TaskItemStatus.Todo] = [TaskItemStatus.InProgress],
        [TaskItemStatus.InProgress] = [TaskItemStatus.Todo, TaskItemStatus.Done],
        [TaskItemStatus.Done] = [TaskItemStatus.InProgress],
    };

    public static bool CanTransition(TaskItemStatus from, TaskItemStatus to) =>
        from == to || Allowed[from].Contains(to);
}
