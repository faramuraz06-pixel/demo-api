using System.Runtime.InteropServices;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace DemoApi.Controllers;

/// <summary>Имитация сбоев для лабораторных по эксплуатации. Включается настройкой Chaos:Enabled.</summary>
[ApiController]
[Route("api/chaos")]
[Tags("Учебные сбои")]
public sealed class ChaosController(IConfiguration config, ILogger<ChaosController> log) : ControllerBase, IActionFilter
{
    private static long _leakedMb;

    [NonAction]
    public void OnActionExecuting(ActionExecutingContext context)
    {
        if (!config.GetValue<bool>("Chaos:Enabled"))
        {
            context.Result = NotFound();
        }
    }

    [NonAction]
    public void OnActionExecuted(ActionExecutedContext context) { }

    /// <summary>Ошибка в запросе: необработанное исключение → 500</summary>
    /// <remarks>Процесс продолжает работать, ошибка пишется в лог с уровнем Error.</remarks>
    [HttpGet("error")]
    public IActionResult Error() =>
        throw new InvalidOperationException("Chaos: искусственная ошибка");

    /// <summary>Медленный ответ</summary>
    /// <param name="ms">Задержка в миллисекундах</param>
    [HttpGet("slow")]
    public async Task<IActionResult> Slow([FromQuery] int ms = 2000, CancellationToken ct = default)
    {
        await Task.Delay(ms, ct);
        return Ok(new { delayedMs = ms });
    }

    /// <summary>Аварийно завершить процесс сервиса (exit code 1)</summary>
    /// <remarks>Контейнер остановится; при restart: unless-stopped Docker запустит его снова.</remarks>
    [HttpPost("crash")]
    public IActionResult Crash()
    {
        log.LogCritical("Chaos: аварийное завершение процесса");
        _ = Task.Run(async () =>
        {
            await Task.Delay(200);
            Environment.Exit(1);
        });
        return Accepted();
    }

    /// <summary>Утечка неуправляемой памяти</summary>
    /// <remarks>Память не освобождается. При лимите контейнера ядро убьёт процесс: OOM, exit code 137.</remarks>
    /// <param name="mb">Сколько мегабайт занять</param>
    [HttpPost("leak")]
    public IActionResult Leak([FromQuery] int mb = 50)
    {
        const int Mb = 1024 * 1024;
        var ptr = Marshal.AllocHGlobal((nint)mb * Mb);
        var chunk = new byte[Mb];
        Array.Fill(chunk, (byte)1);
        for (var i = 0; i < mb; i++)
        {
            Marshal.Copy(chunk, 0, ptr + (nint)i * Mb, Mb);
        }
        return Ok(new { leakedMb = Interlocked.Add(ref _leakedMb, mb) });
    }
}
