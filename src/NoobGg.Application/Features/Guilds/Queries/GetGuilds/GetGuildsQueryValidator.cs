using FluentValidation;

namespace NoobGg.Application.Features.Guilds.Queries.GetGuilds;

public class GetGuildsQueryValidator : AbstractValidator<GetGuildsQuery>
{
    public GetGuildsQueryValidator()
    {
        RuleFor(x => x.Page).GreaterThanOrEqualTo(1);
        RuleFor(x => x.PageSize).InclusiveBetween(1, 50);
    }
}
