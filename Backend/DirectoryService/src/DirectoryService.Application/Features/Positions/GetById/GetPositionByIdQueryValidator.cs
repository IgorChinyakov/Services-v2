using FluentValidation;

namespace DirectoryService.Application.Features.Positions.GetById;

public sealed class GetPositionByIdQueryValidator : AbstractValidator<GetPositionByIdQuery>
{
    public GetPositionByIdQueryValidator()
    {
        RuleFor(query => query.Id).NotEmpty();
    }
}
