using CSharpFunctionalExtensions;
using DirectoryService.Application.Abstractions.Handlers;
using DirectoryService.Application.Abstractions.Repositories;
using DirectoryService.Application.Extensions.Validation;
using DirectoryService.Contracts.Common;
using DirectoryService.Contracts.Departments;
using DirectoryService.Domain.Shared;
using FluentValidation;
using Microsoft.Extensions.Logging;

namespace DirectoryService.Application.Features.Departments.GetChildren;

public sealed class GetDepartmentChildrenHandler
    : IQueryHandler<GetDepartmentChildrenQuery, PagedList<DepartmentNodeDto>>
{
    private readonly IDepartmentQueryRepository _departmentQueryRepository;
    private readonly IValidator<GetDepartmentChildrenQuery> _validator;
    private readonly ILogger<GetDepartmentChildrenHandler> _logger;

    public GetDepartmentChildrenHandler(
        IDepartmentQueryRepository departmentQueryRepository,
        IValidator<GetDepartmentChildrenQuery> validator,
        ILogger<GetDepartmentChildrenHandler> logger)
    {
        _departmentQueryRepository = departmentQueryRepository;
        _validator = validator;
        _logger = logger;
    }

    public async Task<Result<PagedList<DepartmentNodeDto>, Error>> HandleAsync(
        GetDepartmentChildrenQuery query,
        CancellationToken cancellationToken)
    {
        var validationResult = await _validator.ValidateAsync(query, cancellationToken);
        if (!validationResult.IsValid)
        {
            _logger.LogWarning(
                "Department children query validation failed. Errors: {@ValidationErrors}",
                validationResult.Errors);

            return validationResult.ToError();
        }

        return await _departmentQueryRepository.GetChildrenAsync(query, cancellationToken);
    }
}
