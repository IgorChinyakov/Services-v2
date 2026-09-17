using CSharpFunctionalExtensions;
using DirectoryService.Application.Abstractions.Cache;
using DirectoryService.Application.Abstractions.Database;
using DirectoryService.Application.Abstractions.Handlers;
using DirectoryService.Application.Abstractions.Repositories;
using DirectoryService.Application.Extensions.Validation;
using DirectoryService.Domain.Entities.Ids;
using DirectoryService.Domain.Shared;
using FluentValidation;
using Microsoft.Extensions.Logging;

namespace DirectoryService.Application.Features.Positions.Delete;

public sealed class DeletePositionHandler : ICommandHandler<DeletePositionCommand>
{
    private readonly IPositionRepository _positionRepository;
    private readonly ITransactionManager _transactionManager;
    private readonly IValidator<DeletePositionCommand> _validator;
    private readonly ILogger<DeletePositionHandler> _logger;
    private readonly ICacheInvalidator _cacheInvalidator;

    public DeletePositionHandler(
        IPositionRepository positionRepository,
        ITransactionManager transactionManager,
        IValidator<DeletePositionCommand> validator,
        ILogger<DeletePositionHandler> logger,
        ICacheInvalidator cacheInvalidator)
    {
        _positionRepository = positionRepository;
        _transactionManager = transactionManager;
        _validator = validator;
        _logger = logger;
        _cacheInvalidator = cacheInvalidator;
    }

    public async Task<UnitResult<Error>> Handle(
        DeletePositionCommand command,
        CancellationToken cancellationToken)
    {
        var validationResult = await _validator.ValidateAsync(command, cancellationToken);
        if (!validationResult.IsValid)
            return validationResult.ToError();

        var positionResult = await _positionRepository.GetActiveByIdAsync(
            PositionId.Create(command.PositionId),
            cancellationToken);
        if (positionResult.IsFailure)
            return positionResult.Error;

        var deleteResult = _positionRepository.Delete(positionResult.Value);
        if (deleteResult.IsFailure)
            return deleteResult.Error;

        var saveResult = await _transactionManager.SaveChangesAsync(
            "position",
            new { command.PositionId },
            cancellationToken);
        if (saveResult.IsFailure)
            return saveResult.Error;

        _logger.LogInformation(
            "Position deleted successfully. PositionId {PositionId}",
            command.PositionId);

        await _cacheInvalidator.InvalidateAsync([CacheConstants.DEPARTMENTS_CACHE_TAG]);

        return UnitResult.Success<Error>();
    }
}
