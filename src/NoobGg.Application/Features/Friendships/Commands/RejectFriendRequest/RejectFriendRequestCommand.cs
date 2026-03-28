using MediatR;
using NoobGg.Application.Common.Models;

namespace NoobGg.Application.Features.Friendships.Commands.RejectFriendRequest;

public class RejectFriendRequestCommand : IRequest<Result>
{
    public string FriendshipId { get; set; } = string.Empty;
}
