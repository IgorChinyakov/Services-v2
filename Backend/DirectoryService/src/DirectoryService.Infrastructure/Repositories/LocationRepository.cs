using CSharpFunctionalExtensions;
using DirectoryService.Application.Abstractions.Repositories;
using DirectoryService.Domain.Entities;
using DirectoryService.Domain.Shared;
using DirectoryService.Infrastructure.Database;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace DirectoryService.Infrastructure.Repositories;

public sealed class LocationRepository : ILocationRepository
{
    private readonly DirectoryServiceDbContext _dbContext;
    private readonly DbOperationExecutor _dbOperationExecutor;
    private readonly ILogger<LocationRepository> _logger;

    public LocationRepository(
        DirectoryServiceDbContext dbContext,
        DbOperationExecutor dbOperationExecutor,
        ILogger<LocationRepository> logger)
    {
        _dbContext = dbContext;
        _dbOperationExecutor = dbOperationExecutor;
        _logger = logger;
    }

    public async Task<Result<Location, Error>> AddAsync(Location location, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(location);

        return await _dbOperationExecutor.ExecuteSaveAsync(
            async ct =>
            {
                _logger.LogDebug(
                    "Persisting location {LocationId} to database",
                    location.Id.Value);

                await _dbContext.Locations.AddAsync(location, ct);
                await _dbContext.SaveChangesAsync(ct);

                _logger.LogInformation(
                    "Location {LocationId} successfully persisted to database",
                    location.Id.Value);

                return location;
            },
            () => Detach(location),
            "location",
            new { LocationId = location.Id.Value },
            cancellationToken,
            "Location with the same name or address already exists.");
    }

    private void Detach(Location location)
    {
        var entry = _dbContext.Entry(location);
        if (entry.State != EntityState.Detached)
            entry.State = EntityState.Detached;
    }
}
