using FluentValidation;

namespace NoobGg.Application.Features.Guilds.Commands.ApproveJoinRequest;

public class ApproveJoinRequestCommandValidator : AbstractValidator<ApproveJoinRequestCommand>
{
    public ApproveJoinRequestCommandValidator()
    {
        RuleFor(x => x.JoinRequestId).NotEmpty().WithMessage("JoinRequestId is required");
    }
}
