using MediatR;
using NoobGg.Application.Common.Models;

namespace NoobGg.Application.Features.Friendships.Commands.RemoveFriend;

public class RemoveFriendCommand : IRequest<Result>
{
    public string UserId { get; set; } = string.Empty;
}
