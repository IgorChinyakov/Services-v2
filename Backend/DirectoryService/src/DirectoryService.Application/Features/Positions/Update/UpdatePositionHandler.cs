using CSharpFunctionalExtensions;
using DirectoryService.Application.Abstractions.Cache;
using DirectoryService.Application.Abstractions.Database;
using DirectoryService.Application.Abstractions.Handlers;
using DirectoryService.Application.Abstractions.Repositories;
using DirectoryService.Application.Extensions.Validation;
using DirectoryService.Domain.Entities.Ids;
using DirectoryService.Domain.Errors;
using DirectoryService.Domain.Shared;
using DirectoryService.Domain.ValueObjects.Position;
using FluentValidation;
using Microsoft.Extensions.Logging;

namespace DirectoryService.Application.Features.Positions.Update;

public sealed class UpdatePositionHandler : ICommandHandler<UpdatePositionCommand>
{
    private readonly IPositionRepository _positionRepository;
    private readonly ITransactionManager _transactionManager;
    private readonly IValidator<UpdatePositionCommand> _validator;
    private readonly ILogger<UpdatePositionHandler> _logger;
    private readonly ICacheInvalidator _cacheInvalidator;

    public UpdatePositionHandler(
        IPositionRepository positionRepository,
        ITransactionManager transactionManager,
        IValidator<UpdatePositionCommand> validator,
        ILogger<UpdatePositionHandler> logger,
        ICacheInvalidator cacheInvalidator)
    {
        _positionRepository = positionRepository;
        _transactionManager = transactionManager;
        _validator = validator;
        _logger = logger;
        _cacheInvalidator = cacheInvalidator;
    }

    public async Task<UnitResult<Error>> Handle(
        UpdatePositionCommand command,
        CancellationToken cancellationToken)
    {
        var validationResult = await _validator.ValidateAsync(command, cancellationToken);
        if (!validationResult.IsValid)
            return validationResult.ToError();

        var nameResult = Name.Create(command.Name);
        if (nameResult.IsFailure)
            return nameResult.Error;

        var positionId = PositionId.Create(command.PositionId);
        var positionResult = await _positionRepository.GetActiveByIdAsync(positionId, cancellationToken);
        if (positionResult.IsFailure)
            return positionResult.Error;

        var nameExistsResult = await _positionRepository.ActiveNameExistsAsync(
            nameResult.Value.Value,
            positionId,
            cancellationToken);
        if (nameExistsResult.IsFailure)
            return nameExistsResult.Error;

        if (nameExistsResult.Value)
            return PositionErrors.NameDuplicate(nameof(UpdatePositionCommand.Name));

        positionResult.Value.UpdateName(nameResult.Value);

        var saveResult = await _transactionManager.SaveChangesAsync(
            "position",
            new { command.PositionId },
            cancellationToken);
        if (saveResult.IsFailure)
            return saveResult.Error;

        _logger.LogInformation(
            "Position updated successfully. PositionId {PositionId}",
            command.PositionId);

        await _cacheInvalidator.InvalidateAsync([CacheConstants.DEPARTMENTS_CACHE_TAG]);

        return UnitResult.Success<Error>();
    }
}
