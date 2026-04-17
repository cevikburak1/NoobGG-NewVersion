using FluentValidation;

namespace NoobGg.Application.Features.Matchmaking.Commands.JoinMatchQueue;

public class JoinMatchQueueCommandValidator : AbstractValidator<JoinMatchQueueCommand>
{
    public JoinMatchQueueCommandValidator()
    {
        RuleFor(x => x.GameId).NotEmpty().WithMessage("GameId is required");
    }
}
