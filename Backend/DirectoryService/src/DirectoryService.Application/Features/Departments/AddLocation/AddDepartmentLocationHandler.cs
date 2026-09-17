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

namespace DirectoryService.Application.Features.Departments.AddLocation;

public sealed class AddDepartmentLocationHandler : ICommandHandler<AddDepartmentLocationCommand>
{
    private readonly IDepartmentRepository _departmentRepository;
    private readonly ILocationRepository _locationRepository;
    private readonly ITransactionManager _transactionManager;
    private readonly IValidator<AddDepartmentLocationCommand> _validator;
    private readonly ILogger<AddDepartmentLocationHandler> _logger;
    private readonly ICacheInvalidator _cacheInvalidator;

    public AddDepartmentLocationHandler(
        IDepartmentRepository departmentRepository,
        ILocationRepository locationRepository,
        ITransactionManager transactionManager,
        IValidator<AddDepartmentLocationCommand> validator,
        ILogger<AddDepartmentLocationHandler> logger,
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
        AddDepartmentLocationCommand command,
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

        var linkExistsResult = await _departmentRepository.LocationLinkExistsAsync(
            departmentId,
            locationId,
            cancellationToken);
        if (linkExistsResult.IsFailure)
            return linkExistsResult.Error;

        if (linkExistsResult.Value)
            return DepartmentErrors.LocationAlreadyLinked();

        var addResult = await _departmentRepository.AddLocationLinkAsync(
            departmentId,
            locationId,
            cancellationToken);
        if (addResult.IsFailure)
            return addResult.Error;

        var saveResult = await _transactionManager.SaveChangesAsync(
            "department location",
            new { command.DepartmentId, command.LocationId },
            cancellationToken);
        if (saveResult.IsFailure)
            return saveResult.Error;

        _logger.LogInformation(
            "Location linked to department successfully. DepartmentId {DepartmentId}. LocationId {LocationId}",
            command.DepartmentId,
            command.LocationId);

        await _cacheInvalidator.InvalidateAsync(
            [CacheConstants.LOCATIONS_CACHE_TAG, CacheConstants.DEPARTMENTS_CACHE_TAG]);

        return UnitResult.Success<Error>();
    }
}
