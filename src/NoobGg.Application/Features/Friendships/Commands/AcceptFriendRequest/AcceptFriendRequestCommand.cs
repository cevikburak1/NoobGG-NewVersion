using MediatR;
using NoobGg.Application.Common.Models;

namespace NoobGg.Application.Features.Friendships.Commands.AcceptFriendRequest;

public class AcceptFriendRequestCommand : IRequest<Result>
{
    public string FriendshipId { get; set; } = string.Empty;
}
