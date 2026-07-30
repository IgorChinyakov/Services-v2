using DirectoryService.Application.Abstractions.Handlers;
using DirectoryService.Application.Features.Departments.Create;
using DirectoryService.Application.Features.Departments.GetTopByPositions;
using DirectoryService.Application.Features.Departments.UpdateLocations;
using DirectoryService.Application.Features.Departments.UpdateParent;
using DirectoryService.Application.Features.Locations.Create;
using DirectoryService.Application.Features.Locations.Get;
using DirectoryService.Application.Features.Positions.Create;
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
        services.AddScoped<ICommandHandler<CreateDepartmentCommand, Guid>, CreateDepartmentHandler>();
        services.AddScoped<ICommandHandler<UpdateDepartmentLocationsCommand>, UpdateDepartmentLocationsHandler>();
        services.AddScoped<ICommandHandler<UpdateDepartmentParentCommand>, UpdateDepartmentParentHandler>();
        services.AddScoped<ICommandHandler<CreatePositionCommand, Guid>, CreatePositionHandler>();
        services.AddScoped<IQueryHandler<GetLocationsQuery, PagedList<LocationDto>>, GetLocationsHandler>();
        services.AddScoped<
            IQueryHandler<GetTopDepartmentsByPositionsQuery, IReadOnlyList<TopDepartmentByPositionsDto>>,
            GetTopDepartmentsByPositionsHandler>();
        services.AddValidatorsFromAssemblyContaining<CreateLocationCommandValidator>();

        return services;
    }
}
