# Dapper Sandbox

Мини-песочница для практики SQL-запросов через Dapper.

## Запуск

Сначала подними PostgreSQL из корня проекта:

```bash
docker compose up -d postgres
```

Потом запусти песочницу:

```bash
dotnet run --project sandbox/DapperSandbox/DapperSandbox.csproj
```

По умолчанию используется строка подключения:

```text
Host=localhost;Port=5438;Database=postgres;Username=postgres;Password=postgres
```

Если нужна другая БД, задай переменную окружения:

```bash
DAPPER_PLAYGROUND_CONNECTION="Host=localhost;Port=5438;Database=postgres;Username=postgres;Password=postgres" \
dotnet run --project sandbox/DapperSandbox/DapperSandbox.csproj
```

## Как это работает

Перед каждой проверкой пересоздается отдельная схема `dapper_playground`.
Основные таблицы сервиса не трогаются.

Задания находятся в `Program.cs` и идут от простого к более сложному:

- `ExecuteScalarAsync`
- `QuerySingleAsync`
- `QueryAsync`
- `ExecuteAsync`
- параметры и массивы
- `join`
- `group by`
- `DynamicParameters`
- пагинация
- транзакции
- multi-mapping
