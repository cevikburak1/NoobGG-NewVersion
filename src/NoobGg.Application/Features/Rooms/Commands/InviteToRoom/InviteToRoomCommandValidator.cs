using FluentValidation;

namespace NoobGg.Application.Features.Rooms.Commands.InviteToRoom;

public class InviteToRoomCommandValidator : AbstractValidator<InviteToRoomCommand>
{
    public InviteToRoomCommandValidator()
    {
        RuleFor(x => x.RoomId).NotEmpty().WithMessage("RoomId is required");
        RuleFor(x => x.InvitedUserId).NotEmpty().WithMessage("InvitedUserId is required");
    }
}
