using System.ComponentModel.DataAnnotations;
using DemoApi.Domain;
using DemoApi.Services;
using Microsoft.AspNetCore.Mvc;

namespace DemoApi.Controllers;

/// <summary>Задачи: создание, фильтры, изменение, смена статуса, удаление.</summary>
[ApiController]
[Route("api/tasks")]
[Produces("application/json")]
[Tags("Задачи")]
public sealed class TasksController(TaskService tasks) : ControllerBase
{
    /// <summary>Список задач с фильтрами</summary>
    /// <remarks>Пример: <c>GET /api/tasks?projectId=1&amp;status=InProgress</c>. Без фильтров — все задачи.</remarks>
    /// <param name="projectId">Только задачи этого проекта</param>
    /// <param name="status">Только задачи в этом статусе: Todo, InProgress, Done</param>
    /// <response code="200">Задачи по порядку создания</response>
    [HttpGet]
    public Task<IReadOnlyList<TaskItem>> List(
        [FromQuery] int? projectId, [FromQuery] TaskItemStatus? status, CancellationToken ct) =>
        tasks.ListAsync(projectId, status, ct);

    /// <summary>Задача по идентификатору</summary>
    /// <param name="id">Идентификатор задачи</param>
    /// <response code="200">Задача найдена</response>
    /// <response code="404">Задачи с таким id нет</response>
    [HttpGet("{id:int}")]
    [ProducesResponseType<TaskItem>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<TaskItem>> Get(int id, CancellationToken ct)
    {
        var task = await tasks.GetAsync(id, ct);
        if (task is null)
        {
            return NotFound();
        }
        return task;
    }

    /// <summary>Создать задачу в проекте</summary>
    /// <remarks>Новая задача всегда в статусе Todo.</remarks>
    /// <response code="201">Задача создана; её адрес — в заголовке Location</response>
    /// <response code="400">Пустое название или неверные данные</response>
    /// <response code="404">Проекта с таким id нет</response>
    [HttpPost]
    [ProducesResponseType<TaskItem>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<TaskItem>> Create(CreateTaskRequest request, CancellationToken ct)
    {
        var task = await tasks.CreateAsync(request.ProjectId, request.Title, request.Description, request.DueDate, ct);
        if (task is null)
        {
            return Problem($"Проект {request.ProjectId} не найден", statusCode: StatusCodes.Status404NotFound);
        }
        return CreatedAtAction(nameof(Get), new { id = task.Id }, task);
    }

    /// <summary>Изменить название, описание и срок задачи</summary>
    /// <param name="id">Идентификатор задачи</param>
    /// <param name="request">Новые значения</param>
    /// <response code="200">Задача изменена</response>
    /// <response code="400">Пустое название или неверные данные</response>
    /// <response code="404">Задачи с таким id нет</response>
    [HttpPut("{id:int}")]
    [ProducesResponseType<TaskItem>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<TaskItem>> Update(int id, UpdateTaskRequest request, CancellationToken ct)
    {
        var task = await tasks.UpdateAsync(id, request.Title, request.Description, request.DueDate, ct);
        if (task is null)
        {
            return NotFound();
        }
        return task;
    }

    /// <summary>Сменить статус задачи</summary>
    /// <remarks>Разрешены переходы Todo → InProgress → Done и на шаг назад. Todo → Done сразу нельзя.</remarks>
    /// <param name="id">Идентификатор задачи</param>
    /// <param name="request">Новый статус</param>
    /// <response code="200">Статус изменён</response>
    /// <response code="400">Статус не передан или неизвестен</response>
    /// <response code="404">Задачи с таким id нет</response>
    /// <response code="409">Такой переход статуса запрещён</response>
    [HttpPatch("{id:int}/status")]
    [ProducesResponseType<TaskItem>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<TaskItem>> ChangeStatus(int id, ChangeStatusRequest request, CancellationToken ct)
    {
        var task = await tasks.GetAsync(id, ct);
        if (task is null)
        {
            return NotFound();
        }

        var status = request.Status!.Value;
        if (!StatusRules.CanTransition(task.Status, status))
        {
            return Problem($"Нельзя перевести задачу {id} из {task.Status} в {status}", statusCode: StatusCodes.Status409Conflict);
        }

        var updated = await tasks.ChangeStatusAsync(task, status, ct);
        if (updated is null)
        {
            return NotFound();
        }
        return updated;
    }

    /// <summary>Удалить задачу</summary>
    /// <param name="id">Идентификатор задачи</param>
    /// <response code="204">Задача удалена</response>
    /// <response code="404">Задачи с таким id нет</response>
    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        if (!await tasks.DeleteAsync(id, ct))
        {
            return NotFound();
        }
        return NoContent();
    }
}

// Что клиент присылает в теле запроса. Атрибуты — правила проверки: нарушение → 400 автоматически.
// Id, статуса и дат создания здесь нет: их задаёт сервер, и клиент не может их подменить

/// <summary>Создание задачи</summary>
/// <param name="ProjectId">Проект, в котором создаётся задача</param>
/// <param name="Title">Название, до 200 символов</param>
/// <param name="Description">Описание, до 2000 символов</param>
/// <param name="DueDate">Срок, например 2026-12-31</param>
public sealed record CreateTaskRequest(
    [Range(1, int.MaxValue)] int ProjectId,
    [Required, StringLength(200)] string Title,
    [StringLength(2000)] string? Description,
    DateOnly? DueDate);

/// <summary>Изменение задачи</summary>
/// <param name="Title">Название, до 200 символов</param>
/// <param name="Description">Описание, до 2000 символов</param>
/// <param name="DueDate">Срок, например 2026-12-31</param>
public sealed record UpdateTaskRequest(
    [Required, StringLength(200)] string Title,
    [StringLength(2000)] string? Description,
    DateOnly? DueDate);

/// <summary>Смена статуса</summary>
/// <param name="Status">Новый статус: Todo, InProgress, Done</param>
public sealed record ChangeStatusRequest(
    [Required] TaskItemStatus? Status);
