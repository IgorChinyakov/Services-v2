using FluentValidation;

namespace DirectoryService.Application.Features.Locations.GetById;

public sealed class GetLocationByIdQueryValidator : AbstractValidator<GetLocationByIdQuery>
{
    public GetLocationByIdQueryValidator()
    {
        RuleFor(query => query.Id).NotEmpty();
    }
}
