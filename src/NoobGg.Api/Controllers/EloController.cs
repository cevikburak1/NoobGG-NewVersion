using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NoobGg.Application.Features.Elo.Commands.RecordMatchResult;
using NoobGg.Application.Features.Elo.Commands.SubmitSessionResults;
using NoobGg.Application.Features.Elo.Queries.GetEloHistory;
using NoobGg.Application.Features.Elo.Queries.GetLeaderboard;

namespace NoobGg.Api.Controllers;

[Route("api/elo")]
[Authorize]
public class EloController : ApiControllerBase
{
    [HttpPost("match")]
    public async Task<IActionResult> RecordMatch([FromBody] RecordMatchResultCommand command)
    {
        var result = await Mediator.Send(command);
        return HandleResult(result);
    }

    [HttpGet("leaderboard")]
    [AllowAnonymous]
    public async Task<IActionResult> GetLeaderboard(
        [FromQuery] string gameId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50)
    {
        var result = await Mediator.Send(new GetLeaderboardQuery
        {
            GameId = gameId,
            Page = page,
            PageSize = pageSize
        });
        return HandleResult(result);
    }

    [HttpGet("history/{userId}/{gameId}")]
    [AllowAnonymous]
    public async Task<IActionResult> GetEloHistory(string userId, string gameId)
    {
        var result = await Mediator.Send(new GetEloHistoryQuery
        {
            UserId = userId,
            GameId = gameId
        });
        return HandleResult(result);
    }

    [HttpPost("session-results")]
    public async Task<IActionResult> SubmitSessionResults([FromBody] SubmitSessionResultsCommand command)
    {
        var result = await Mediator.Send(command);
        return HandleResult(result);
    }
}
