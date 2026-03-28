using MediatR;
using NoobGg.Application.Common.Models;
using NoobGg.Application.Features.Friendships.DTOs;

namespace NoobGg.Application.Features.Friendships.Queries.GetMyFriends;

public class GetMyFriendsQuery : IRequest<Result<List<FriendshipResponse>>>
{
}
