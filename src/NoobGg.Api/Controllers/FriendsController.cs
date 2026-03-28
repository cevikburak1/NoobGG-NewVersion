using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NoobGg.Application.Features.Friendships.Commands.AcceptFriendRequest;
using NoobGg.Application.Features.Friendships.Commands.RejectFriendRequest;
using NoobGg.Application.Features.Friendships.Commands.RemoveFriend;
using NoobGg.Application.Features.Friendships.Commands.SendFriendRequest;
using NoobGg.Application.Features.Friendships.Queries.GetMyFriends;
using NoobGg.Application.Features.Friendships.Queries.GetPendingRequests;

namespace NoobGg.Api.Controllers;

[Route("api/friends")]
[Authorize]
public class FriendsController : ApiControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetMyFriends()
    {
        var result = await Mediator.Send(new GetMyFriendsQuery());
        return HandleResult(result);
    }

    [HttpGet("requests")]
    public async Task<IActionResult> GetPendingRequests()
    {
        var result = await Mediator.Send(new GetPendingRequestsQuery());
        return HandleResult(result);
    }

    [HttpPost("request/{userId}")]
    public async Task<IActionResult> SendFriendRequest(string userId)
    {
        var result = await Mediator.Send(new SendFriendRequestCommand { AddresseeId = userId });
        return HandleResult(result);
    }

    [HttpPost("accept/{friendshipId}")]
    public async Task<IActionResult> AcceptRequest(string friendshipId)
    {
        var result = await Mediator.Send(new AcceptFriendRequestCommand { FriendshipId = friendshipId });
        return HandleResult(result);
    }

    [HttpPost("reject/{friendshipId}")]
    public async Task<IActionResult> RejectRequest(string friendshipId)
    {
        var result = await Mediator.Send(new RejectFriendRequestCommand { FriendshipId = friendshipId });
        return HandleResult(result);
    }

    [HttpDelete("{userId}")]
    public async Task<IActionResult> RemoveFriend(string userId)
    {
        var result = await Mediator.Send(new RemoveFriendCommand { UserId = userId });
        return HandleResult(result);
    }
}
