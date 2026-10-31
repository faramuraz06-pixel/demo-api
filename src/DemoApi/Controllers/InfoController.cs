using Microsoft.AspNetCore.Mvc;

namespace DemoApi.Controllers;

/// <summary>Служебная информация о запущенном экземпляре сервиса.</summary>
[ApiController]
[Route("")]
[Tags("Сервис")]
public sealed class InfoController(IConfiguration config) : ControllerBase
{
    /// <summary>Какая версия сервиса запущена и на каком контейнере</summary>
    /// <remarks>Версия — sha коммита, из которого собран образ (тема 5). Host — имя контейнера.</remarks>
    /// <response code="200">Версия и хост</response>
    [HttpGet]
    public InfoResponse Get() =>
        new("DemoApi", config["APP_VERSION"] ?? "dev", Environment.MachineName);
}

/// <summary>Информация о запущенном сервисе</summary>
/// <param name="Service">Имя сервиса</param>
/// <param name="Version">Версия: sha коммита, из которого собран образ, или dev</param>
/// <param name="Host">Имя хоста — в контейнере это его id</param>
public sealed record InfoResponse(string Service, string Version, string Host);
