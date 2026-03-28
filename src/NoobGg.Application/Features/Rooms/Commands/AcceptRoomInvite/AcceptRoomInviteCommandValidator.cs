using FluentValidation;

namespace NoobGg.Application.Features.Rooms.Commands.AcceptRoomInvite;

public class AcceptRoomInviteCommandValidator : AbstractValidator<AcceptRoomInviteCommand>
{
    public AcceptRoomInviteCommandValidator()
    {
        RuleFor(x => x.InviteId).NotEmpty().WithMessage("InviteId is required");
    }
}
