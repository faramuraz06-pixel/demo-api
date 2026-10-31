# DemoApi — сервис задач (эталонное решение курса)

Так выглядит репозиторий студента после лабы 8.

## Запуск из Visual Studio — сразу, без БД

Откройте `DemoApi.slnx`, профиль **http**, **F5**. Сервис стартует с хранилищем в памяти (`"Storage": "InMemory"` в `appsettings.Development.json`), браузер откроет Swagger: http://localhost:8080/swagger. Тесты (`dotnet test`) в этом режиме тоже работают без БД.

## Запуск из Visual Studio на PostgreSQL

1. Поднимите БД в контейнере. Если порт 5432 занят (на ПК стоит свой PostgreSQL), используйте 15432:
   ```powershell
   Copy-Item .env.example .env
   Add-Content .env "DB_PORT=15432"          # только если 5432 занят
   docker compose up -d --wait db
   ```
2. В `src/DemoApi/appsettings.Development.json`:
   ```json
   "Storage": "Postgres",
   "ConnectionStrings": {
     "Db": "Host=localhost;Port=15432;Database=demo;Username=demo;Password=demo"
   },
   ```
   `Port` — тот же, что `DB_PORT` (по умолчанию 5432). Логин и пароль — из `.env` / `compose.yaml`.

   Вместо правки JSON можно задать переменные окружения в `Properties/launchSettings.json` → `environmentVariables`: `"Storage": "Postgres"` и `"ConnectionStrings__Db": "Host=localhost;Port=15432;..."`. Они переопределяют `appsettings*.json`.
3. **F5.** В консоли — `Хранилище: PostgreSQL`, в Swagger `GET /api/projects` вернёт учебные проекты из `db/seed.sql`.

Ошибка `28P01: password authentication failed` означает, что сервис подключился не к той БД: порт в строке не совпадает с портом контейнера.

## Весь стек в контейнерах

```powershell
docker compose up -d --build --wait                  # сервис + PostgreSQL (Storage=Postgres задан в compose.yaml)
docker compose -f monitoring/compose.yaml up -d      # Prometheus, Grafana, cAdvisor, Dozzle
```

## API

| Запрос | Что делает |
|---|---|
| `GET/POST /api/projects`, `GET/PUT/DELETE /api/projects/{id}` | Проекты (PUT — переименование) |
| `GET /api/projects/{id}/stats` | Сводка задач проекта |
| `GET/POST /api/tasks?projectId=&status=`, `GET/PUT/DELETE /api/tasks/{id}` | Задачи |
| `PATCH /api/tasks/{id}/status` | Смена статуса по правилам `Todo → InProgress → Done` |
| `GET /`, `/health/live`, `/health/ready`, `/metrics` | Версия, здоровье, метрики |
| `/api/chaos/error`, `slow`, `crash`, `leak` | Учебные сбои (`Chaos:Enabled`) |

## Состав

| Путь | Что |
|---|---|
| `STRUCTURE.md` | Карта проекта: что в каждой папке и зачем |
| `src/DemoApi` | Controllers → Services → Data, Domain. Хранилище по настройке `Storage`: `InMemory` (`Data/InMemory`) или `Postgres` (Npgsql). В Development — страница `/swagger` |
| `db/seed.sql` | Схема и учебные данные для PostgreSQL в контейнере |
| `tests/DemoApi.Tests` | `Unit/` — правила; `Integration/` — WebApplicationFactory + PostgreSQL |
| `Dockerfile`, `.dockerignore` | Multi-stage, non-root, HEALTHCHECK |
| `compose.yaml` | API + PostgreSQL, лимиты, рестарты, ротация логов |
| `.github/workflows/ci-cd.yml` | test → image (GHCR) → deploy (self-hosted runner) |
| `.github/dependabot.yml`, `pull_request_template.md` | Настройки репозитория |
| `monitoring/` | Prometheus + алерты, Grafana (datasource и дашборд как код), cAdvisor, Dozzle |
| `scripts/load.ps1` | Фоновая нагрузка для лаб по мониторингу |
