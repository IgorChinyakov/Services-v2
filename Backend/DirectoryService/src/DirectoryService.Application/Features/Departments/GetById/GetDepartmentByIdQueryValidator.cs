using FluentValidation;

namespace DirectoryService.Application.Features.Departments.GetById;

public sealed class GetDepartmentByIdQueryValidator : AbstractValidator<GetDepartmentByIdQuery>
{
    public GetDepartmentByIdQueryValidator()
    {
        RuleFor(query => query.Id).NotEmpty();
    }
}
