using FluentValidation;

namespace NoobGg.Application.Features.Rooms.Commands.JoinRoom;

public class JoinRoomCommandValidator : AbstractValidator<JoinRoomCommand>
{
    public JoinRoomCommandValidator()
    {
        RuleFor(x => x.RoomId)
            .NotEmpty().WithMessage("RoomId is required");
    }
}
