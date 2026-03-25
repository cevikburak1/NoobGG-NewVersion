using MediatR;
using NoobGg.Application.Common.Models;

namespace NoobGg.Application.Features.Rooms.Commands.KickMember;

public record KickMemberCommand : IRequest<Result>
{
    public string RoomId { get; init; } = string.Empty;
    public string UserId { get; init; } = string.Empty;
}
