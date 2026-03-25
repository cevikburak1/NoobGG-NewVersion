using FluentValidation;

namespace NoobGg.Application.Features.Rooms.Commands.LeaveRoom;

public class LeaveRoomCommandValidator : AbstractValidator<LeaveRoomCommand>
{
    public LeaveRoomCommandValidator()
    {
        RuleFor(x => x.RoomId)
            .NotEmpty().WithMessage("RoomId is required");
    }
}
