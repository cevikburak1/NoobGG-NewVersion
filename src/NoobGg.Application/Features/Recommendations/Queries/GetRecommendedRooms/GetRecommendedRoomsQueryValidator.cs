using FluentValidation;

namespace NoobGg.Application.Features.Recommendations.Queries.GetRecommendedRooms;

public class GetRecommendedRoomsQueryValidator : AbstractValidator<GetRecommendedRoomsQuery>
{
    public GetRecommendedRoomsQueryValidator()
    {
        RuleFor(x => x.Limit)
            .InclusiveBetween(1, 50).WithMessage("Limit must be between 1 and 50");
    }
}
