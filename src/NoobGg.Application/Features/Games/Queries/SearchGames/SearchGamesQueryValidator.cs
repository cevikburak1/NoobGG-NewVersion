using FluentValidation;

namespace NoobGg.Application.Features.Games.Queries.SearchGames;

public class SearchGamesQueryValidator : AbstractValidator<SearchGamesQuery>
{
    public SearchGamesQueryValidator()
    {
        RuleFor(x => x.SearchTerm)
            .NotEmpty().WithMessage("Search term is required")
            .MinimumLength(2).WithMessage("Search term must be at least 2 characters")
            .MaximumLength(100).WithMessage("Search term must not exceed 100 characters");

        RuleFor(x => x.Limit)
            .InclusiveBetween(1, 50).WithMessage("Limit must be between 1 and 50");
    }
}
