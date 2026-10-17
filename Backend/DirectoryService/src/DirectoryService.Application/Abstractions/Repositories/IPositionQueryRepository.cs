using CSharpFunctionalExtensions;
using DirectoryService.Application.Features.Positions.Get;
using DirectoryService.Contracts.Positions;
using DirectoryService.Domain.Shared;

namespace DirectoryService.Application.Abstractions.Repositories;

public interface IPositionQueryRepository
{
    Task<Result<PositionCursorPageDto, Error>> GetAsync(
        GetPositionsQuery query,
        CancellationToken cancellationToken);
}
