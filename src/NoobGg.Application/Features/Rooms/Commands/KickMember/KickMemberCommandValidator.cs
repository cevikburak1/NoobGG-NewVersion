using FluentValidation;

namespace NoobGg.Application.Features.Rooms.Commands.KickMember;

public class KickMemberCommandValidator : AbstractValidator<KickMemberCommand>
{
    public KickMemberCommandValidator()
    {
        RuleFor(x => x.RoomId)
            .NotEmpty().WithMessage("RoomId is required");

        RuleFor(x => x.UserId)
            .NotEmpty().WithMessage("UserId is required");
    }
}
