using CSharpFunctionalExtensions;
using DirectoryService.Application.Abstractions.Repositories;
using DirectoryService.Domain.Entities;
using DirectoryService.Domain.Entities.Ids;
using DirectoryService.Domain.Shared;
using DirectoryService.Infrastructure.Database;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace DirectoryService.Infrastructure.Repositories;

public sealed class PositionRepository : IPositionRepository
{
    private readonly DirectoryServiceDbContext _dbContext;
    private readonly DbOperationExecutor _dbOperationExecutor;
    private readonly ILogger<PositionRepository> _logger;

    public PositionRepository(
        DirectoryServiceDbContext dbContext,
        DbOperationExecutor dbOperationExecutor,
        ILogger<PositionRepository> logger)
    {
        _dbContext = dbContext;
        _dbOperationExecutor = dbOperationExecutor;
        _logger = logger;
    }

    public async Task<Result<bool, Error>> ActiveNameExistsAsync(
        string name,
        CancellationToken cancellationToken)
    {
        return await _dbOperationExecutor.ExecuteAsync(
            async ct =>
            {
                var normalizedName = name.Trim();

                return await _dbContext.Positions
                    .AsNoTracking()
                    .AnyAsync(
                        position => position.IsActive && position.Name.Value == normalizedName,
                        ct);
            },
            "check active position name uniqueness",
            new { PositionName = name },
            cancellationToken);
    }

    public async Task<Result<IReadOnlyCollection<DepartmentId>, Error>> GetMissingActiveDepartmentIdsAsync(
        IReadOnlyCollection<DepartmentId> departmentIds,
        CancellationToken cancellationToken)
    {
        return await _dbOperationExecutor.ExecuteAsync(
            async ct =>
            {
                var requestedDepartmentIds = departmentIds.Distinct().ToArray();

                var existingDepartmentIds = await _dbContext.Departments
                    .AsNoTracking()
                    .Where(department => department.IsActive && requestedDepartmentIds.Contains(department.Id))
                    .Select(department => department.Id)
                    .ToArrayAsync(ct);

                IReadOnlyCollection<DepartmentId> missingDepartmentIds = requestedDepartmentIds
                    .Except(existingDepartmentIds)
                    .ToArray();

                return missingDepartmentIds;
            },
            "check active departments",
            new { DepartmentIds = departmentIds.Select(id => id.Value) },
            cancellationToken);
    }

    public async Task<Result<Position, Error>> AddAsync(
        Position position,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(position);

        return await _dbOperationExecutor.ExecuteSaveAsync(
            async ct =>
            {
                _logger.LogDebug(
                    "Persisting position {PositionId} to database",
                    position.Id.Value);

                await _dbContext.Positions.AddAsync(position, ct);
                await _dbContext.SaveChangesAsync(ct);

                _logger.LogInformation(
                    "Position {PositionId} successfully persisted to database",
                    position.Id.Value);

                return position;
            },
            () => Detach(position),
            "position",
            new { PositionId = position.Id.Value },
            cancellationToken,
            "An active position with the same name already exists.");
    }

    private void Detach(Position position)
    {
        foreach (var departmentPosition in position.DepartmentPositions)
            DetachEntry(departmentPosition);

        DetachEntry(position);
    }

    private void DetachEntry(object entity)
    {
        var entry = _dbContext.Entry(entity);
        if (entry.State != EntityState.Detached)
            entry.State = EntityState.Detached;
    }
}
