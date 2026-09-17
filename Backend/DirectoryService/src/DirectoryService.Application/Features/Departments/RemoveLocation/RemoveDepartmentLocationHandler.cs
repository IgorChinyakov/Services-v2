using CSharpFunctionalExtensions;
using DirectoryService.Application.Abstractions.Cache;
using DirectoryService.Application.Abstractions.Database;
using DirectoryService.Application.Abstractions.Handlers;
using DirectoryService.Application.Abstractions.Repositories;
using DirectoryService.Application.Extensions.Validation;
using DirectoryService.Domain.Entities.Ids;
using DirectoryService.Domain.Errors;
using DirectoryService.Domain.Shared;
using FluentValidation;
using Microsoft.Extensions.Logging;

namespace DirectoryService.Application.Features.Departments.RemoveLocation;

public sealed class RemoveDepartmentLocationHandler : ICommandHandler<RemoveDepartmentLocationCommand>
{
    private readonly IDepartmentRepository _departmentRepository;
    private readonly ILocationRepository _locationRepository;
    private readonly ITransactionManager _transactionManager;
    private readonly IValidator<RemoveDepartmentLocationCommand> _validator;
    private readonly ILogger<RemoveDepartmentLocationHandler> _logger;
    private readonly ICacheInvalidator _cacheInvalidator;

    public RemoveDepartmentLocationHandler(
        IDepartmentRepository departmentRepository,
        ILocationRepository locationRepository,
        ITransactionManager transactionManager,
        IValidator<RemoveDepartmentLocationCommand> validator,
        ILogger<RemoveDepartmentLocationHandler> logger,
        ICacheInvalidator cacheInvalidator)
    {
        _departmentRepository = departmentRepository;
        _locationRepository = locationRepository;
        _transactionManager = transactionManager;
        _validator = validator;
        _logger = logger;
        _cacheInvalidator = cacheInvalidator;
    }

    public async Task<UnitResult<Error>> Handle(
        RemoveDepartmentLocationCommand command,
        CancellationToken cancellationToken)
    {
        var validationResult = await _validator.ValidateAsync(command, cancellationToken);
        if (!validationResult.IsValid)
            return validationResult.ToError();

        var departmentId = DepartmentId.Create(command.DepartmentId);
        var locationId = LocationId.Create(command.LocationId);

        var departmentResult = await _departmentRepository.GetActiveByIdAsync(
            departmentId,
            cancellationToken);
        if (departmentResult.IsFailure)
            return departmentResult.Error;

        var locationResult = await _locationRepository.GetActiveByIdAsync(locationId, cancellationToken);
        if (locationResult.IsFailure)
            return locationResult.Error;

        var removeResult = await _departmentRepository.RemoveLocationLinkAsync(
            departmentId,
            locationId,
            cancellationToken);
        if (removeResult.IsFailure)
            return removeResult.Error;

        if (!removeResult.Value)
            return DepartmentErrors.LocationNotLinked();

        var saveResult = await _transactionManager.SaveChangesAsync(
            "department location",
            new { command.DepartmentId, command.LocationId },
            cancellationToken);
        if (saveResult.IsFailure)
            return saveResult.Error;

        _logger.LogInformation(
            "Location unlinked from department successfully. DepartmentId {DepartmentId}. LocationId {LocationId}",
            command.DepartmentId,
            command.LocationId);

        await _cacheInvalidator.InvalidateAsync(
            [CacheConstants.LOCATIONS_CACHE_TAG, CacheConstants.DEPARTMENTS_CACHE_TAG]);

        return UnitResult.Success<Error>();
    }
}
