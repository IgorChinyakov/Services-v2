# Directory Service

## Назначение

Directory Service — единый справочник организационной структуры компании. Сервис хранит подразделения, локации и должности, а также связи между ними.

Основные задачи сервиса:

- хранить иерархию подразделений;
- хранить адреса и часовые зоны локаций;
- хранить должности;
- поддерживать связи `Department-Location` и `Department-Position`;
- предоставлять единый API с одинаковым форматом ответов и ошибок;
- безопасно изменять дерево подразделений при параллельных запросах;
- скрывать soft-deleted данные из обычных read-запросов;
- очищать старые неактивные данные фоновыми задачами.

## Архитектура backend

Backend разделён на проекты по Clean Architecture:

- `DirectoryService.Domain` — сущности, value objects, доменные правила, `Result` и `Error`;
- `DirectoryService.Application` — commands, queries, handlers, validators и абстракции репозиториев;
- `DirectoryService.Infrastructure` — EF Core, PostgreSQL, Dapper read-repositories, транзакции, Redis и Quartz;
- `DirectoryService.Contracts` — request-модели и DTO API;
- `DirectoryService.Presentation` — controllers, middleware, преобразование результата в HTTP-ответ.

Основная цепочка запроса:

```text
HTTP -> Controller -> Handler -> Repository/ReadDbContext -> PostgreSQL
                                      |
                                      +-> Result/Error -> EndpointResult -> Envelope
```

Запись обычно выполняется через EF Core и `TransactionManager`. Сложные read-запросы используют `IReadDbContext` или Dapper query repositories.

## Доменные модели

### Department

`Department` — отдел, команда или филиал компании.

Ключевые поля:

- `Id`;
- `Name`;
- `Slug` — стабильный идентификатор и сегмент materialized path;
- `ParentId` и `Children`;
- `Path` — путь в дереве, хранится как PostgreSQL `ltree`;
- `Depth` — глубина относительно корня;
- `IsActive` и `DeletedAt`;
- даты создания и изменения.

Структура дерева построена на adjacency list (`ParentId`) и materialized path (`Path`). Для поиска поддеревьев используется `ltree` и GiST-индекс.

### Location

`Location` — локация, к которой могут быть привязаны подразделения.

Содержит имя, адрес, timezone, активность и даты создания/изменения. Имя и адрес защищены уникальными ограничениями.

### Position

`Position` — должность или роль. Содержит имя, необязательное описание, активность и даты. Имя уникально среди активных должностей.

### Связи

- `DepartmentLocation` связывает подразделения и локации;
- `DepartmentPosition` связывает подразделения и должности.

Связи сохраняются для истории, но глобальные EF Core query filters и ручные фильтры Dapper не позволяют обычным API показывать неактивные бизнес-данные.

## API backend

Все маршруты имеют префикс `/api`, а успешные и ошибочные ответы возвращаются через `Envelope`.

### Locations

- `POST /api/locations` — создать локацию;
- `GET /api/locations` — получить список с фильтрами, сортировкой и offset-пагинацией;
- `GET /api/locations/{id}` — получить локацию по id;
- `GET /api/locations/top` — получить топ локаций по количеству подразделений;
- `PATCH /api/locations/{id}` — изменить локацию;
- `DELETE /api/locations/{id}` — soft delete локации.

### Departments

- `POST /api/departments` — создать корневое или дочернее подразделение;
- `GET /api/departments` — список с поиском, сортировкой и offset-пагинацией;
- `GET /api/departments/{id}` — получить подразделение по id;
- `GET /api/departments/tree` — получить корневые узлы дерева;
- `GET /api/departments/{id}/children` — получить непосредственных детей;
- `GET /api/departments/{id}/ancestors` — получить цепочку предков;
- `GET /api/departments/tree/search` — поиск узлов с цепочкой предков;
- `GET /api/departments/top-positions` — топ подразделений по количеству должностей;
- `PATCH /api/departments/{id}` — изменить подразделение;
- `PUT /api/departments/{id}/locations` — заменить список локаций;
- `POST/DELETE /api/departments/{departmentId}/locations/{locationId}` — добавить или удалить связь;
- `POST/DELETE /api/departments/{departmentId}/positions/{positionId}` — добавить или удалить связь;
- `PUT /api/departments/{id}/parent` — перенести подразделение в другое место дерева;
- `DELETE /api/departments/{id}` — soft delete подразделения.

### Positions

- `POST /api/positions` — создать должность;
- `PATCH /api/positions/{id}` — переименовать должность;
- `DELETE /api/positions/{id}` — soft delete должности.

## Контракт ответа и ошибки

Успешный ответ имеет форму `Envelope<T>`:

```json
{
  "result": {},
  "error": null,
  "isError": false,
  "timeGenerated": "..."
}
```

Для списков `result` содержит `PagedList<T>` с полями `items`, `page`, `pageSize`, `totalCount`, `totalPages`, `hasPreviousPage` и `hasNextPage`.

Ошибки проходят цепочку `Result -> EndpointResult -> Envelope`. Типичные статусы:

- `400` — ошибка валидации;
- `404` — сущность не найдена или неактивна;
- `409` — нарушение бизнес-правила или уникального ограничения;
- `500` — непредвиденная ошибка.

Ошибки PostgreSQL по уникальным индексам преобразуются в предметные error codes.

## Транзакции и конкурентность

`TransactionManager` отвечает за открытие транзакции, `SaveChangesAsync`, commit и rollback. Репозитории изменяют состояние `DbContext` или выполняют SQL, но не управляют границами транзакции самостоятельно.

Перенос подразделения выполняется одной транзакцией:

1. участники операции блокируются через `FOR UPDATE`;
2. записи блокируются в детерминированном порядке;
3. проверяется, что новый родитель не находится в переносимом поддереве;
4. массово пересчитываются `ParentId`, `Path` и `Depth`;
5. выполняется commit или rollback.

## Soft delete и фоновые задачи

Soft delete не удаляет запись сразу: устанавливаются `IsActive = false` и `DeletedAt`.

Quartz jobs периодически очищают старые записи:

- `DepartmentCleanupJob` — удаляет старые подразделения и обновляет пути потомков;
- `LocationCleanupJob` — удаляет старые локации и их связи;
- `PositionCleanupJob` — удаляет старые должности и их связи.

Очистка выполняется batch-ами, с блокировками и транзакциями. Более новые soft-deleted записи не затрагиваются.

## Кэширование

Для read-запросов используется cache-aside через `HybridCache` и Redis:

1. handler формирует ключ с параметрами запроса;
2. cache возвращает результат при cache hit;
3. при miss данные загружаются из БД и сохраняются с TTL;
4. команды создания, изменения и удаления инвалидируют связанные cache tags.

Redis запускается через Docker Compose. При недоступности кэша основной read-сценарий должен продолжать работу через базу данных.

## Инфраструктура

Локальное окружение содержит:

- PostgreSQL;
- Redis;
- Seq;
- backend container;
- Nginx как внешнюю точку входа.

Миграции EF Core находятся в `src/DirectoryService.Infrastructure/Migrations`. Производительность SQL-запросов проверяется через `EXPLAIN ANALYZE`, отдельные материалы и seed-скрипты находятся в `docs` и `scripts`.

Запуск backend-инфраструктуры:

```bash
cp .env.example .env
docker compose up -d --build
```

## Интеграционные тесты

Тесты используют `WebApplicationFactory`, Testcontainers с PostgreSQL и Respawn для очистки состояния между тестами. Покрываются создание, чтение, обновление, soft delete, связи, дерево, перенос, кэширование, cleanup и конкурентные сценарии.

Запуск:

```bash
dotnet test tests/DirectoryService.IntegrationTests/DirectoryService.IntegrationTests.csproj
```

## Frontend

Frontend находится в `Frontend/directory-service` и строится на:

- Next.js App Router;
- React и TypeScript;
- Tailwind CSS;
- shadcn/ui;
- Feature-Sliced Design;
- TanStack Query;
- Axios.

Базовые слои FSD:

- `app` — маршруты, layout и провайдеры;
- `widgets` — крупные UI-блоки, например header, sidebar и списки;
- `features` — пользовательские действия;
- `entities` — модели и API конкретных сущностей;
- `shared` — HTTP-клиент, маршруты, UI-kit и общие утилиты.

Уже подготовлено:

- админский shell с header и sidebar;
- маршруты `/locations`, `/departments`, `/positions` и `/playground`;
- Axios-клиент и общие типы `Envelope`, `ApiError`, `PagedResult`;
- загрузка локаций через entity API и TanStack Query;
- состояния loading, error, empty и data для списка локаций;
- базовые DTO Department и заготовка widget списка подразделений;
- общий UI-слой с компонентами shadcn/ui.

Frontend пока не завершён. В ближайшем порядке предстоит:

1. завершить актуальные Department DTO под `slug` и offset-пагинацию;
2. реализовать `fetchDepartments` с разворачиванием `Envelope`;
3. подключить `DepartmentsList` через TanStack Query;
4. добавить поиск, сортировку, пагинацию и состояния списка отделов;
5. подключить дерево Department: roots, children, ancestors и search;
6. добавить entities и страницы для Position;
7. реализовать формы и mutations для создания, изменения, связей и soft delete;
8. добавить обработку ошибок backend и инвалидировать query cache после mutations;
9. проверить работу frontend через Nginx и Docker Compose после стабилизации локальной сборки.

Frontend сейчас нельзя считать завершённым: часть страниц остаётся заглушками, а widget Department находится в процессе разработки и ещё требует исправления TypeScript/React-кода.

## Текущие ограничения

- frontend и backend пока развиваются неравномерно;
- frontend Department DTO ещё синхронизируются с актуальным backend-контрактом;
- полная пользовательская навигация по дереву и mutations ещё не подключена;
- Docker Compose сейчас сфокусирован на backend-инфраструктуре, пока frontend не проходит production build.

## Ближайшая цель

Сначала завершить read-side frontend для Department: DTO, API-функцию, query, таблицу или карточки, фильтры и пагинацию. После этого подключить tree API, затем mutations и формы для управления справочником.

