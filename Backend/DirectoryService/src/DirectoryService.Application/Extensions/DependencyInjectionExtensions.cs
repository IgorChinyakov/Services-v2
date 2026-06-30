using DirectoryService.Application.Abstractions.Handlers;
using DirectoryService.Application.Features.Departments.Create;
using DirectoryService.Application.Features.Locations.Create;
using DirectoryService.Application.Features.Positions.Create;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;

namespace DirectoryService.Application.Extensions;

public static class DependencyInjectionExtensions
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<ICommandHandler<CreateLocationCommand, Guid>, CreateLocationHandler>();
        services.AddScoped<ICommandHandler<CreateDepartmentCommand, Guid>, CreateDepartmentHandler>();
        services.AddScoped<ICommandHandler<CreatePositionCommand, Guid>, CreatePositionHandler>();
        services.AddValidatorsFromAssemblyContaining<CreateLocationCommandValidator>();

        return services;
    }
}
