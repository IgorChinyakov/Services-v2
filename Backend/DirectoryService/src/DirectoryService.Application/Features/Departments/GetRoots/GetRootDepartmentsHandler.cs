using CSharpFunctionalExtensions;
using DirectoryService.Application.Abstractions.Handlers;
using DirectoryService.Application.Abstractions.Repositories;
using DirectoryService.Application.Extensions.Validation;
using DirectoryService.Contracts.Common;
using DirectoryService.Contracts.Departments;
using DirectoryService.Domain.Shared;
using FluentValidation;
using Microsoft.Extensions.Logging;

namespace DirectoryService.Application.Features.Departments.GetRoots;

public sealed class GetRootDepartmentsHandler
    : IQueryHandler<GetRootDepartmentsQuery, PagedList<RootDepartmentDto>>
{
    private readonly IDepartmentQueryRepository _departmentQueryRepository;
    private readonly IValidator<GetRootDepartmentsQuery> _validator;
    private readonly ILogger<GetRootDepartmentsHandler> _logger;

    public GetRootDepartmentsHandler(
        IDepartmentQueryRepository departmentQueryRepository,
        IValidator<GetRootDepartmentsQuery> validator,
        ILogger<GetRootDepartmentsHandler> logger)
    {
        _departmentQueryRepository = departmentQueryRepository;
        _validator = validator;
        _logger = logger;
    }

    public async Task<Result<PagedList<RootDepartmentDto>, Error>> HandleAsync(
        GetRootDepartmentsQuery query,
        CancellationToken cancellationToken)
    {
        var validationResult = await _validator.ValidateAsync(query, cancellationToken);
        if (!validationResult.IsValid)
        {
            _logger.LogWarning(
                "Root departments query validation failed. Errors: {@ValidationErrors}",
                validationResult.Errors);

            return validationResult.ToError();
        }

        return await _departmentQueryRepository.GetRootsAsync(query, cancellationToken);
    }
}
