using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NoobGg.Application.Features.Matchmaking.Commands.JoinMatchQueue;
using NoobGg.Application.Features.Matchmaking.Commands.LeaveMatchQueue;
using NoobGg.Application.Features.Matchmaking.Queries.GetMatchQueueStatus;

namespace NoobGg.Api.Controllers;

[Authorize]
[Route("api/matchmaking")]
public class MatchmakingController : ApiControllerBase
{
    [HttpPost("queue")]
    public async Task<IActionResult> JoinQueue([FromBody] JoinMatchQueueCommand command)
    {
        var result = await Mediator.Send(command);
        return HandleResult(result);
    }

    [HttpDelete("queue")]
    public async Task<IActionResult> LeaveQueue()
    {
        var result = await Mediator.Send(new LeaveMatchQueueCommand());
        return HandleResult(result);
    }

    [HttpGet("queue/status")]
    public async Task<IActionResult> QueueStatus()
    {
        var result = await Mediator.Send(new GetMatchQueueStatusQuery());
        return HandleResult(result);
    }
}
