using DirectoryService.Application.Abstractions.Handlers;
using DirectoryService.Application.Locations.Create;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;

namespace DirectoryService.Application.Extensions;

public static class DependencyInjectionExtensions
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<ICommandHandler<CreateLocationCommand, Guid>, CreateLocationHandler>();
        services.AddValidatorsFromAssemblyContaining<CreateLocationCommandValidator>();

        return services;
    }
}