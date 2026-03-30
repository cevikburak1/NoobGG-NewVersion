using FluentValidation;

namespace NoobGg.Application.Features.Guilds.Commands.RejectJoinRequest;

public class RejectJoinRequestCommandValidator : AbstractValidator<RejectJoinRequestCommand>
{
    public RejectJoinRequestCommandValidator()
    {
        RuleFor(x => x.JoinRequestId).NotEmpty().WithMessage("JoinRequestId is required");
    }
}
