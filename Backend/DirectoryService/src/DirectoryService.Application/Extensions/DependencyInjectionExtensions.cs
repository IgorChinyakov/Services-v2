using DirectoryService.Application.Abstractions.Handlers;
using DirectoryService.Application.Features.Departments.AddLocation;
using DirectoryService.Application.Features.Departments.AddPosition;
using DirectoryService.Application.Features.Departments.CleanupInactive;
using DirectoryService.Application.Features.Departments.Create;
using DirectoryService.Application.Features.Departments.Get;
using DirectoryService.Application.Features.Departments.GetById;
using DirectoryService.Application.Features.Departments.GetChildren;
using DirectoryService.Application.Features.Departments.GetRoots;
using DirectoryService.Application.Features.Departments.GetTopByPositions;
using DirectoryService.Application.Features.Departments.RemoveLocation;
using DirectoryService.Application.Features.Departments.RemovePosition;
using DirectoryService.Application.Features.Departments.SoftDelete;
using DirectoryService.Application.Features.Departments.Update;
using DirectoryService.Application.Features.Departments.UpdateLocations;
using DirectoryService.Application.Features.Departments.UpdateParent;
using DirectoryService.Application.Features.Locations.CleanupInactive;
using DirectoryService.Application.Features.Locations.Create;
using DirectoryService.Application.Features.Locations.Delete;
using DirectoryService.Application.Features.Locations.Get;
using DirectoryService.Application.Features.Locations.GetById;
using DirectoryService.Application.Features.Locations.Update;
using DirectoryService.Application.Features.Positions.CleanupInactive;
using DirectoryService.Application.Features.Positions.Create;
using DirectoryService.Application.Features.Positions.Delete;
using DirectoryService.Application.Features.Positions.Update;
using DirectoryService.Contracts.Common;
using DirectoryService.Contracts.Departments;
using DirectoryService.Contracts.Locations;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;

namespace DirectoryService.Application.Extensions;

public static class DependencyInjectionExtensions
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<ICommandHandler<CreateLocationCommand, Guid>, CreateLocationHandler>();
        services.AddScoped<ICommandHandler<UpdateLocationCommand>, UpdateLocationHandler>();
        services.AddScoped<ICommandHandler<DeleteLocationCommand>, DeleteLocationHandler>();
        services.AddScoped<ICommandHandler<CreateDepartmentCommand, Guid>, CreateDepartmentHandler>();
        services.AddScoped<ICommandHandler<UpdateDepartmentCommand>, UpdateDepartmentHandler>();
        services.AddScoped<ICommandHandler<AddDepartmentLocationCommand>, AddDepartmentLocationHandler>();
        services.AddScoped<ICommandHandler<RemoveDepartmentLocationCommand>, RemoveDepartmentLocationHandler>();
        services.AddScoped<ICommandHandler<AddDepartmentPositionCommand>, AddDepartmentPositionHandler>();
        services.AddScoped<ICommandHandler<RemoveDepartmentPositionCommand>, RemoveDepartmentPositionHandler>();
        services.AddScoped<ICommandHandler<UpdateDepartmentLocationsCommand>, UpdateDepartmentLocationsHandler>();
        services.AddScoped<ICommandHandler<UpdateDepartmentParentCommand>, UpdateDepartmentParentHandler>();
        services.AddScoped<ICommandHandler<SoftDeleteDepartmentCommand>, SoftDeleteDepartmentHandler>();
        services.AddScoped<ICommandHandler<CreatePositionCommand, Guid>, CreatePositionHandler>();
        services.AddScoped<ICommandHandler<UpdatePositionCommand>, UpdatePositionHandler>();
        services.AddScoped<ICommandHandler<DeletePositionCommand>, DeletePositionHandler>();
        services.AddScoped<
            ICommandHandler<CleanupInactiveLocationsCommand, int>,
            CleanupInactiveLocationsHandler>();
        services.AddScoped<
            ICommandHandler<CleanupInactivePositionsCommand, int>,
            CleanupInactivePositionsHandler>();
        services.AddScoped<
            ICommandHandler<CleanupInactiveDepartmentsCommand, int>,
            CleanupInactiveDepartmentsHandler>();
        services.AddScoped<IQueryHandler<GetLocationsQuery, PagedList<LocationListItemDto>>, GetLocationsHandler>();
        services.AddScoped<
            IQueryHandler<GetLocationByIdQuery, LocationDetailsDto>,
            GetLocationByIdHandler>();
        services.AddScoped<
            IQueryHandler<GetDepartmentByIdQuery, DepartmentDetailsDto>,
            GetDepartmentByIdHandler>();
        services.AddScoped<
            IQueryHandler<GetDepartmentsQuery, PagedList<DepartmentListItemDto>>,
            GetDepartmentsHandler>();
        services.AddScoped<
            IQueryHandler<GetRootDepartmentsQuery, PagedList<RootDepartmentDto>>,
            GetRootDepartmentsHandler>();
        services.AddScoped<
            IQueryHandler<GetDepartmentChildrenQuery, PagedList<DepartmentNodeDto>>,
            GetDepartmentChildrenHandler>();
        services.AddScoped<
            IQueryHandler<GetTopDepartmentsByPositionsQuery, IReadOnlyList<TopDepartmentByPositionsDto>>,
            GetTopDepartmentsByPositionsHandler>();
        services.AddValidatorsFromAssemblyContaining<CreateLocationCommandValidator>();

        return services;
    }
}
