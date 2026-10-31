-- Учебные данные для сервиса задач.
-- PostgreSQL выполняет скрипты из /docker-entrypoint-initdb.d только при первой инициализации пустого тома.
-- Схема та же, что создаёт сервис при старте (src/DemoApi/Data/DatabaseInitializer.cs).

CREATE TABLE IF NOT EXISTS projects (
    id          serial PRIMARY KEY,
    name        text        NOT NULL UNIQUE,
    created_at  timestamptz NOT NULL DEFAULT now()
);

CREATE TABLE IF NOT EXISTS tasks (
    id          serial PRIMARY KEY,
    project_id  int         NOT NULL REFERENCES projects (id) ON DELETE CASCADE,
    title       text        NOT NULL,
    description text        NULL,
    status      text        NOT NULL DEFAULT 'todo' CHECK (status IN ('todo', 'in_progress', 'done')),
    due_date    date        NULL,
    created_at  timestamptz NOT NULL DEFAULT now(),
    updated_at  timestamptz NOT NULL DEFAULT now()
);

CREATE INDEX IF NOT EXISTS ix_tasks_project_status ON tasks (project_id, status);

INSERT INTO projects (name) VALUES
    ('Курс DevOps'),
    ('Домашние дела');

INSERT INTO tasks (project_id, title, status, due_date) VALUES
    (1, 'Установить Docker Desktop',  'done',        NULL),
    (1, 'Запустить БД в контейнере',  'in_progress', current_date + 1),
    (1, 'Написать Dockerfile',        'todo',        current_date + 7),
    (2, 'Купить продукты',            'todo',        current_date - 1);
