using CSharpFunctionalExtensions;
using DirectoryService.Application.Abstractions.Database;
using DirectoryService.Application.Abstractions.Handlers;
using DirectoryService.Application.Extensions.Validation;
using DirectoryService.Contracts.Departments;
using DirectoryService.Domain.Entities.Ids;
using DirectoryService.Domain.Shared;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace DirectoryService.Application.Features.Departments.GetById;

public sealed class GetDepartmentByIdHandler
    : IQueryHandler<GetDepartmentByIdQuery, DepartmentDetailsDto>
{
    private readonly IReadDbContext _readDbContext;
    private readonly IValidator<GetDepartmentByIdQuery> _validator;
    private readonly ILogger<GetDepartmentByIdHandler> _logger;

    public GetDepartmentByIdHandler(
        IReadDbContext readDbContext,
        IValidator<GetDepartmentByIdQuery> validator,
        ILogger<GetDepartmentByIdHandler> logger)
    {
        _readDbContext = readDbContext;
        _validator = validator;
        _logger = logger;
    }

    public async Task<Result<DepartmentDetailsDto, Error>> HandleAsync(
        GetDepartmentByIdQuery query,
        CancellationToken cancellationToken)
    {
        var validationResult = await _validator.ValidateAsync(query, cancellationToken);
        if (!validationResult.IsValid)
            return validationResult.ToError();

        var departmentId = DepartmentId.Create(query.Id);
        var department = await _readDbContext.Departments
            .Where(item => item.Id == departmentId)
            .Select(item => new DepartmentDetailsDto(
                item.Id.Value,
                item.ParentId == null ? null : item.ParentId.Value,
                item.Name.Value,
                item.Identifier.Value,
                item.Path,
                item.Depth,
                item.IsActive,
                item.CreatedAt,
                item.UpdatedAt))
            .FirstOrDefaultAsync(cancellationToken);

        if (department is null)
        {
            _logger.LogWarning("Department {DepartmentId} was not found", query.Id);

            return GeneralErrors.NotFound(
                $"Department with id '{query.Id}' was not found.",
                nameof(query.Id));
        }

        _logger.LogInformation("Department {DepartmentId} was loaded", query.Id);

        return department;
    }
}
