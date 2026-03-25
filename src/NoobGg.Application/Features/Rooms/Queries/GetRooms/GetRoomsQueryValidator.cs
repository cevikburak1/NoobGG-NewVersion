using FluentValidation;

namespace NoobGg.Application.Features.Rooms.Queries.GetRooms;

public class GetRoomsQueryValidator : AbstractValidator<GetRoomsQuery>
{
    public GetRoomsQueryValidator()
    {
        RuleFor(x => x.Page)
            .GreaterThanOrEqualTo(1).WithMessage("Page must be at least 1");

        RuleFor(x => x.PageSize)
            .InclusiveBetween(1, 50).WithMessage("PageSize must be between 1 and 50");

        RuleFor(x => x.Status)
            .IsInEnum().WithMessage("Invalid status")
            .When(x => x.Status.HasValue);

        RuleFor(x => x.Region)
            .IsInEnum().WithMessage("Invalid region")
            .When(x => x.Region.HasValue);

        RuleFor(x => x.Language)
            .IsInEnum().WithMessage("Invalid language")
            .When(x => x.Language.HasValue);
    }
}
