using FluentValidation;

namespace NoobGg.Application.Features.Rooms.Commands.CloseRoom;

public class CloseRoomCommandValidator : AbstractValidator<CloseRoomCommand>
{
    public CloseRoomCommandValidator()
    {
        RuleFor(x => x.RoomId)
            .NotEmpty().WithMessage("RoomId is required");
    }
}
