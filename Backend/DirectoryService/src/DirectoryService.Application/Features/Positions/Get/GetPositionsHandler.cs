using CSharpFunctionalExtensions;
using DirectoryService.Application.Abstractions.Handlers;
using DirectoryService.Application.Abstractions.Repositories;
using DirectoryService.Application.Extensions.Validation;
using DirectoryService.Contracts.Positions;
using DirectoryService.Domain.Shared;
using FluentValidation;

namespace DirectoryService.Application.Features.Positions.Get;

public sealed class GetPositionsHandler : IQueryHandler<GetPositionsQuery, PositionCursorPageDto>
{
    private readonly IValidator<GetPositionsQuery> _validator;
    private readonly IPositionQueryRepository _repository;

    public GetPositionsHandler(
        IValidator<GetPositionsQuery> validator,
        IPositionQueryRepository repository)
    {
        _validator = validator;
        _repository = repository;
    }

    public async Task<Result<PositionCursorPageDto, Error>> HandleAsync(
        GetPositionsQuery query,
        CancellationToken cancellationToken)
    {
        var validationResult = await _validator.ValidateAsync(query, cancellationToken);
        if (!validationResult.IsValid)
            return validationResult.ToError();

        return await _repository.GetAsync(query, cancellationToken);
    }
}
