using CSharpFunctionalExtensions;
using DirectoryService.Application.Abstractions.Database;
using DirectoryService.Application.Abstractions.Handlers;
using DirectoryService.Application.Extensions.Validation;
using DirectoryService.Contracts.Positions;
using DirectoryService.Domain.Entities.Ids;
using DirectoryService.Domain.Errors;
using DirectoryService.Domain.Shared;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace DirectoryService.Application.Features.Positions.GetById;

public sealed class GetPositionByIdHandler(
    IReadDbContext readDbContext,
    IValidator<GetPositionByIdQuery> validator)
    : IQueryHandler<GetPositionByIdQuery, PositionListItemDto>
{
    public async Task<Result<PositionListItemDto, Error>> HandleAsync(
        GetPositionByIdQuery query,
        CancellationToken cancellationToken)
    {
        var validationResult = await validator.ValidateAsync(query, cancellationToken);
        if (!validationResult.IsValid)
            return validationResult.ToError();

        var positionId = PositionId.Create(query.Id);
        var position = await readDbContext.Positions
            .Where(item => item.Id == positionId && item.DeletedAt == null)
            .Select(item => new PositionListItemDto(
                item.Id.Value,
                item.Name.Value,
                item.Description == null ? null : item.Description.Value,
                item.CreatedAt))
            .FirstOrDefaultAsync(cancellationToken);

        if (position is null)
            return PositionErrors.NotFound(query.Id, nameof(query.Id));

        return position;
    }
}
