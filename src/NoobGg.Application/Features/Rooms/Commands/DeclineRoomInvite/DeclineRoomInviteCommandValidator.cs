using FluentValidation;

namespace NoobGg.Application.Features.Rooms.Commands.DeclineRoomInvite;

public class DeclineRoomInviteCommandValidator : AbstractValidator<DeclineRoomInviteCommand>
{
    public DeclineRoomInviteCommandValidator()
    {
        RuleFor(x => x.InviteId).NotEmpty().WithMessage("InviteId is required");
    }
}
