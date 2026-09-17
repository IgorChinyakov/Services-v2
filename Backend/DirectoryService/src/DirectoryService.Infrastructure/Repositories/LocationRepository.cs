using CSharpFunctionalExtensions;
using DirectoryService.Application.Abstractions.Repositories;
using DirectoryService.Domain.Entities;
using DirectoryService.Domain.Shared;
using DirectoryService.Infrastructure.Database;
using DirectoryService.Infrastructure.Errors;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace DirectoryService.Infrastructure.Repositories;

public sealed class LocationRepository : ILocationRepository
{
    private readonly DirectoryServiceDbContext _dbContext;
    private readonly ILogger<LocationRepository> _logger;

    public LocationRepository(
        DirectoryServiceDbContext dbContext,
        ILogger<LocationRepository> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<Result<bool, Error>> NameExistsAsync(
        string name,
        CancellationToken cancellationToken)
    {
        try
        {
            var normalizedName = name.Trim();

            return await _dbContext.Locations
                .AsNoTracking()
                .AnyAsync(
                    location => location.Name.Value == normalizedName,
                    cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            _logger.LogError(
                exception,
                "Failed to check location name uniqueness. LocationName {LocationName}",
                name);

            return DatabaseErrors.OperationFailed("check location name uniqueness");
        }
    }

    public async Task<Result<Location, Error>> AddAsync(Location location, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(location);

        _logger.LogDebug(
            "Adding location {LocationId} to change tracker",
            location.Id.Value);

        await _dbContext.Locations.AddAsync(location, cancellationToken);

        return location;
    }
}
