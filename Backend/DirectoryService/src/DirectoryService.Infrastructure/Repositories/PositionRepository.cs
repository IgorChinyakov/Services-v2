using CSharpFunctionalExtensions;
using DirectoryService.Application.Abstractions.Repositories;
using DirectoryService.Domain.Entities;
using DirectoryService.Domain.Entities.Ids;
using DirectoryService.Domain.Shared;
using DirectoryService.Infrastructure.Database;
using DirectoryService.Infrastructure.Errors;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace DirectoryService.Infrastructure.Repositories;

public sealed class PositionRepository : IPositionRepository
{
    private readonly DirectoryServiceDbContext _dbContext;
    private readonly ILogger<PositionRepository> _logger;

    public PositionRepository(
        DirectoryServiceDbContext dbContext,
        ILogger<PositionRepository> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<Result<bool, Error>> ActiveNameExistsAsync(
        string name,
        CancellationToken cancellationToken)
    {
        try
        {
            var normalizedName = name.Trim();

            return await _dbContext.Positions
                .AsNoTracking()
                .AnyAsync(
                    position => position.IsActive && position.Name.Value == normalizedName,
                    cancellationToken);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception exception)
        {
            _logger.LogError(
                exception,
                "Failed to check active position name uniqueness. PositionName {PositionName}",
                name);

            return DatabaseErrors.OperationFailed("check active position name uniqueness");
        }
    }

    public async Task<Result<IReadOnlyCollection<DepartmentId>, Error>> GetMissingActiveDepartmentIdsAsync(
        IReadOnlyCollection<DepartmentId> departmentIds,
        CancellationToken cancellationToken)
    {
        try
        {
            var requestedDepartmentIds = departmentIds.Distinct().ToArray();
            var missingDepartmentIds = new List<DepartmentId>();

            foreach (var departmentId in requestedDepartmentIds)
            {
                var exists = await _dbContext.Departments
                    .AsNoTracking()
                    .AnyAsync(
                        department => department.IsActive && department.Id == departmentId,
                        cancellationToken);

                if (!exists)
                    missingDepartmentIds.Add(departmentId);
            }

            return Result.Success<IReadOnlyCollection<DepartmentId>, Error>(missingDepartmentIds);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception exception)
        {
            _logger.LogError(
                exception,
                "Failed to check active departments. DepartmentIds {@DepartmentIds}",
                departmentIds.Select(id => id.Value));

            return DatabaseErrors.OperationFailed("check active departments");
        }
    }

    public async Task<Result<Position, Error>> AddAsync(
        Position position,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(position);

        _logger.LogDebug(
            "Adding position {PositionId} to change tracker",
            position.Id.Value);

        await _dbContext.Positions.AddAsync(position, cancellationToken);

        return position;
    }
}
