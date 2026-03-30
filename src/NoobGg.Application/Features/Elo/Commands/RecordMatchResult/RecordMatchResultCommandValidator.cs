using FluentValidation;

namespace NoobGg.Application.Features.Elo.Commands.RecordMatchResult;

public class RecordMatchResultCommandValidator : AbstractValidator<RecordMatchResultCommand>
{
    public RecordMatchResultCommandValidator()
    {
        RuleFor(x => x.GameId).NotEmpty();
        RuleFor(x => x.OpponentId).NotEmpty();
    }
}
