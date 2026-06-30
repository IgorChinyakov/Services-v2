using CSharpFunctionalExtensions;
using DirectoryService.Application.Abstractions.Repositories;
using DirectoryService.Domain.Entities;
using DirectoryService.Domain.Entities.Ids;
using DirectoryService.Domain.Shared;
using DirectoryService.Infrastructure.Database;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace DirectoryService.Infrastructure.Repositories;

public sealed class DepartmentRepository : IDepartmentRepository
{
    private readonly DirectoryServiceDbContext _dbContext;
    private readonly DbOperationExecutor _dbOperationExecutor;
    private readonly ILogger<DepartmentRepository> _logger;

    public DepartmentRepository(
        DirectoryServiceDbContext dbContext,
        DbOperationExecutor dbOperationExecutor,
        ILogger<DepartmentRepository> logger)
    {
        _dbContext = dbContext;
        _dbOperationExecutor = dbOperationExecutor;
        _logger = logger;
    }

    public async Task<Result<bool, Error>> IdentifierExistsAsync(
        string identifier,
        CancellationToken cancellationToken)
    {
        return await _dbOperationExecutor.ExecuteAsync(
            async ct =>
            {
                var normalizedIdentifier = identifier.Trim();

                return await _dbContext.Departments
                    .AsNoTracking()
                    .AnyAsync(
                        department => department.Identifier.Value == normalizedIdentifier,
                        ct);
            },
            "check department identifier uniqueness",
            new { DepartmentIdentifier = identifier },
            cancellationToken);
    }

    public async Task<Result<Department, Error>> GetActiveByIdAsync(
        DepartmentId departmentId,
        CancellationToken cancellationToken)
    {
        return await _dbOperationExecutor.ExecuteResultAsync<Department>(
            async ct =>
            {
                var department = await _dbContext.Departments
                    .FirstOrDefaultAsync(
                        item => item.Id == departmentId && item.IsActive,
                        ct);

                if (department is null)
                    return GeneralErrors.NotFound("Department does not exist or is inactive.");

                return department;
            },
            "load active department",
            new { DepartmentId = departmentId.Value },
            cancellationToken);
    }

    public async Task<Result<IReadOnlyCollection<LocationId>, Error>> GetMissingActiveLocationIdsAsync(
        IReadOnlyCollection<LocationId> locationIds,
        CancellationToken cancellationToken)
    {
        return await _dbOperationExecutor.ExecuteAsync<IReadOnlyCollection<LocationId>>(
            async ct =>
            {
                var requestedLocationIds = locationIds.Distinct().ToArray();

                var existingLocationIds = await _dbContext.Locations
                    .AsNoTracking()
                    .Where(location => location.IsActive && requestedLocationIds.Contains(location.Id))
                    .Select(location => location.Id)
                    .ToArrayAsync(ct);

                IReadOnlyCollection<LocationId> missingLocationIds = requestedLocationIds
                    .Except(existingLocationIds)
                    .ToArray();

                return missingLocationIds;
            },
            "check active locations",
            new { LocationIds = locationIds.Select(id => id.Value) },
            cancellationToken);
    }

    public async Task<Result<Department, Error>> AddAsync(
        Department department,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(department);

        return await _dbOperationExecutor.ExecuteSaveAsync(
            async ct =>
            {
                _logger.LogDebug(
                    "Persisting department {DepartmentId} to database",
                    department.Id.Value);

                await _dbContext.Departments.AddAsync(department, ct);
                await _dbContext.SaveChangesAsync(ct);

                _logger.LogInformation(
                    "Department {DepartmentId} successfully persisted to database",
                    department.Id.Value);

                return department;
            },
            () => Detach(department),
            "department",
            new { DepartmentId = department.Id.Value },
            cancellationToken);
    }

    private void Detach(Department department)
    {
        foreach (var departmentLocation in department.DepartmentLocations)
            DetachEntry(departmentLocation);

        DetachEntry(department);
    }

    private void DetachEntry(object entity)
    {
        var entry = _dbContext.Entry(entity);
        if (entry.State != EntityState.Detached)
            entry.State = EntityState.Detached;
    }
}
