using FluentValidation;

namespace NoobGg.Application.Features.Elo.Commands.SubmitSessionResults;

public class SubmitSessionResultsCommandValidator : AbstractValidator<SubmitSessionResultsCommand>
{
    public SubmitSessionResultsCommandValidator()
    {
        RuleFor(x => x.RoomId).NotEmpty();
        RuleFor(x => x.Wins).GreaterThanOrEqualTo(0);
        RuleFor(x => x.Losses).GreaterThanOrEqualTo(0);
        RuleFor(x => x)
            .Must(x => x.Wins + x.Losses > 0)
            .WithMessage("At least one match result is required");
    }
}
