# Directory Service

Directory Service - единый справочник организационной структуры компании. Сервис хранит подразделения, локации и должности, чтобы другие внутренние сервисы могли использовать один источник правды вместо дублирования справочников в своих базах.

## Назначение

Сервис отвечает за:

- хранение дерева подразделений компании;
- хранение локаций, в которых работают подразделения;
- хранение должностей и их привязок к подразделениям;
- поддержание консистентных связей между подразделениями, локациями и позициями;
- стандартизированные API-ответы через `Envelope`;
- возврат ошибок через `Result` и доменные `Error`.

## Архитектура

Проект разделен по слоям Clean Architecture:

- `DirectoryService.Domain` - доменные сущности, value objects, ids, `Result`/`Error`;
- `DirectoryService.Application` - use cases, commands, handlers, validators, repository abstractions;
- `DirectoryService.Infrastructure` - EF Core, PostgreSQL, repositories, migrations, transactions;
- `DirectoryService.Presentation` - Web API controllers, middleware, API response mapping;
- `DirectoryService.Contracts` - request/DTO-контракты, не зависящие от остальных проектов.

Запрос проходит по цепочке:

`Controller -> Handler -> Repository -> Database`

## Доменные модели

### Department

`Department` - подразделение, отдел, филиал или команда компании.

Основные поля:

- `Id` - `DepartmentId`;
- `Name` - value object с ограничением длины;
- `Identifier` - value object, используется в `Path`;
- `ParentId` / `Parent` - связь с родительским подразделением;
- `Children` - дочерние подразделения;
- `Path` - materialized path дерева, хранится в PostgreSQL как `ltree`;
- `Depth` - глубина в дереве;
- `IsActive` - soft-delete флаг;
- `CreatedAt`, `UpdatedAt`;
- `DepartmentLocations` - связи с локациями;
- `DepartmentPositions` - связи с должностями.

Дерево подразделений реализовано через materialized path. Для ускорения операций по поддереву используется PostgreSQL extension `ltree` и GiST-индекс по `path`.

### Location

`Location` - физическое или логическое место, где находятся подразделения.

Основные поля:

- `Id` - `LocationId`;
- `Name` - value object, уникальное имя локации;
- `Address` - value object с полями `Country`, `City`, `Street`, `Building`;
- `TimeZone` - value object с IANA timezone;
- `IsActive`;
- `CreatedAt`, `UpdatedAt`;
- `DepartmentLocations` - связи с подразделениями.

Для локаций настроены уникальные ограничения на имя и адрес.

### Position

`Position` - должность или роль в компании.

Основные поля:

- `Id` - `PositionId`;
- `Name` - value object, уникально среди активных должностей;
- `Description` - optional value object;
- `IsActive`;
- `CreatedAt`, `UpdatedAt`;
- `DepartmentPositions` - связи с подразделениями.

### DepartmentLocation

Связующая сущность many-to-many между `Department` и `Location`.

Используется, чтобы подразделение могло работать в нескольких локациях, а одна локация могла относиться к нескольким подразделениям.

### DepartmentPosition

Связующая сущность many-to-many между `Department` и `Position`.

Используется, чтобы должность могла быть доступна в нескольких подразделениях, а подразделение могло иметь несколько должностей.

## Реализованные API-сценарии

### Locations

- `POST /api/locations` - создание локации.
- Валидация входных данных через FluentValidation.
- В валидаторах используются `Validate`-методы value object-ов.
- Возвращается id созданной локации.
- Ошибки БД и уникальных индексов преобразуются в `Error`.

### Departments

- `POST /api/departments` - создание подразделения.
- Поддерживается создание root-подразделения и дочернего подразделения.
- Проверяется существование активных `locationIds`.
- Формируются `Path` и `Depth`.
- `Identifier` уникален.
- `PUT /api/departments/{departmentId}/locations` - полная замена списка локаций подразделения.
- Проверяется активность подразделения и всех переданных локаций.
- Обновление выполняется в транзакции.
- `PUT /api/departments/{departmentId}/parent` - перенос подразделения в другое место дерева.
- Поддерживается перенос под нового родителя и перенос в root через `parentId: null`.
- Проверяется, что подразделение существует и активно.
- Проверяется, что новый родитель существует и активен.
- Запрещен перенос подразделения под самого себя.
- Запрещен перенос подразделения под собственного потомка.
- Поддерево блокируется pessimistic lock через `FOR UPDATE`.
- `parent_id`, `path` и `depth` обновляются массовым SQL-запросом на стороне PostgreSQL.
- Все изменения выполняются в одной транзакции.

### Positions

- `POST /api/positions` - создание должности.
- Проверяется уникальность имени среди активных должностей.
- Проверяется существование активных подразделений.
- Создается связь с каждым переданным department id.

## Инфраструктура

Реализовано:

- PostgreSQL в Docker Compose;
- Seq в Docker Compose;
- EF Core DbContext и конфигурации сущностей;
- snake_case naming через явную конфигурацию, без EF naming conventions library;
- миграции EF Core;
- PostgreSQL extension `ltree`;
- GiST-индекс для дерева подразделений;
- `TransactionManager` и `TransactionScope` поверх EF Core transaction;
- централизованное сохранение через `ITransactionManager.SaveChangesAsync`;
- структурное логирование через Serilog;
- HTTP request logging;
- exception middleware с логированием исключений и возвратом `Envelope`;
- общий контракт handlers: `ICommandHandler` и `IQueryHandler`.

## API-ответы

Все контроллеры возвращают результат через `EndpointResult`, который преобразует `Result`/`Error` в `Envelope`.

Успешные операции возвращают HTTP `200`.

Типовые ошибки:

- `400` - validation error;
- `404` - entity not found;
- `409` - conflict/business rule violation;
- `500` - infrastructure или unexpected error.

## Проверенный статус

Последняя проверка выполнялась на локальной PostgreSQL из `docker-compose.yml`.

Проверено:

- сборка проекта: `dotnet build DirectoryService.sln --no-restore -v q`;
- создание локации;
- создание дерева подразделений;
- перенос подразделения под другого родителя;
- перенос подразделения в root;
- пересчет `parent_id`, `path`, `depth` в БД;
- запрет self-parent;
- запрет переноса под собственного потомка;
- `404` для отсутствующего parent;
- `404` для отсутствующего department.

Текущий фокус следующего этапа - продолжать расширять CRUD-сценарии и при необходимости покрывать сложные операции интеграционными тестами.
