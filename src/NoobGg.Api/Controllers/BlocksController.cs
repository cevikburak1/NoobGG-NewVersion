using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NoobGg.Application.Features.Blocks.Commands.BlockUser;
using NoobGg.Application.Features.Blocks.Commands.UnblockUser;
using NoobGg.Application.Features.Blocks.Queries.GetBlockedUsers;

namespace NoobGg.Api.Controllers;

[Route("api/blocks")]
[Authorize]
public class BlocksController : ApiControllerBase
{
    [HttpPost("{userId}")]
    public async Task<IActionResult> Block(string userId)
    {
        var result = await Mediator.Send(new BlockUserCommand { BlockedUserId = userId });
        return HandleResult(result);
    }

    [HttpDelete("{userId}")]
    public async Task<IActionResult> Unblock(string userId)
    {
        var result = await Mediator.Send(new UnblockUserCommand { BlockedUserId = userId });
        return HandleResult(result);
    }

    [HttpGet]
    public async Task<IActionResult> GetBlocked()
    {
        var result = await Mediator.Send(new GetBlockedUsersQuery());
        return HandleResult(result);
    }
}
