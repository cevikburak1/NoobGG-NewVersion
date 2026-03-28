using FluentValidation;

namespace NoobGg.Application.Features.Recommendations.Queries.GetRecommendedPlayers;

public class GetRecommendedPlayersQueryValidator : AbstractValidator<GetRecommendedPlayersQuery>
{
    public GetRecommendedPlayersQueryValidator()
    {
        RuleFor(x => x.Limit)
            .InclusiveBetween(1, 50).WithMessage("Limit must be between 1 and 50");
    }
}
