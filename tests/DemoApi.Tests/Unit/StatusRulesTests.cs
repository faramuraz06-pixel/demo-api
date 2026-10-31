using DemoApi.Domain;

namespace DemoApi.Tests.Unit;

// Юнит-тесты бизнес-правил: без БД и HTTP
public class StatusRulesTests
{
    [Theory]
    [InlineData(TaskItemStatus.Todo, TaskItemStatus.InProgress, true)]
    [InlineData(TaskItemStatus.InProgress, TaskItemStatus.Done, true)]
    [InlineData(TaskItemStatus.InProgress, TaskItemStatus.Todo, true)]
    [InlineData(TaskItemStatus.Done, TaskItemStatus.InProgress, true)]
    [InlineData(TaskItemStatus.Todo, TaskItemStatus.Todo, true)]
    [InlineData(TaskItemStatus.Todo, TaskItemStatus.Done, false)]
    [InlineData(TaskItemStatus.Done, TaskItemStatus.Todo, false)]
    public void CanTransition(TaskItemStatus from, TaskItemStatus to, bool expected) =>
        Assert.Equal(expected, StatusRules.CanTransition(from, to));
}
