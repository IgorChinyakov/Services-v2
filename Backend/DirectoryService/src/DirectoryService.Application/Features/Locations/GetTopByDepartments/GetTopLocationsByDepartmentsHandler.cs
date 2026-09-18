using CSharpFunctionalExtensions;
using DirectoryService.Application.Abstractions.Handlers;
using DirectoryService.Application.Abstractions.Repositories;
using DirectoryService.Contracts.Locations;
using DirectoryService.Domain.Shared;

namespace DirectoryService.Application.Features.Locations.GetTopByDepartments;

public sealed class GetTopLocationsByDepartmentsHandler
    : IQueryHandler<GetTopLocationsByDepartmentsQuery, IReadOnlyList<TopLocationByDepartmentsDto>>
{
    private readonly ILocationQueryRepository _locationQueryRepository;

    public GetTopLocationsByDepartmentsHandler(ILocationQueryRepository locationQueryRepository)
    {
        _locationQueryRepository = locationQueryRepository;
    }

    public Task<Result<IReadOnlyList<TopLocationByDepartmentsDto>, Error>> HandleAsync(
        GetTopLocationsByDepartmentsQuery query,
        CancellationToken cancellationToken)
    {
        return _locationQueryRepository.GetTopByDepartmentsAsync(cancellationToken);
    }
}
