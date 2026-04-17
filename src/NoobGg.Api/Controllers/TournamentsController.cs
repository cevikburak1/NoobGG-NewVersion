using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NoobGg.Application.Features.Tournaments.Commands.CreateTournament;
using NoobGg.Application.Features.Tournaments.Commands.GenerateBracket;
using NoobGg.Application.Features.Tournaments.Commands.JoinTournament;
using NoobGg.Application.Features.Tournaments.Commands.LeaveTournament;
using NoobGg.Application.Features.Tournaments.Commands.ReportMatchResult;
using NoobGg.Application.Features.Tournaments.Queries.GetTournamentDetail;
using NoobGg.Application.Features.Tournaments.Queries.GetTournaments;
using NoobGg.Domain.Enums;

namespace NoobGg.Api.Controllers;

[Authorize]
[Route("api/tournaments")]
public class TournamentsController : ApiControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetTournaments(
        [FromQuery] string? gameId,
        [FromQuery] string? guildId,
        [FromQuery] TournamentStatus? status,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        var result = await Mediator.Send(new GetTournamentsQuery
        {
            GameId = gameId,
            GuildId = guildId,
            Status = status,
            Page = page,
            PageSize = pageSize
        });
        return HandleResult(result);
    }

    [HttpGet("{tournamentId}")]
    public async Task<IActionResult> GetDetail(string tournamentId)
    {
        var result = await Mediator.Send(new GetTournamentDetailQuery { TournamentId = tournamentId });
        return HandleResult(result);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateTournamentCommand command)
    {
        var result = await Mediator.Send(command);
        return HandleResult(result);
    }

    [HttpPost("{tournamentId}/join")]
    public async Task<IActionResult> Join(string tournamentId)
    {
        var result = await Mediator.Send(new JoinTournamentCommand { TournamentId = tournamentId });
        return HandleResult(result);
    }

    [HttpPost("{tournamentId}/leave")]
    public async Task<IActionResult> Leave(string tournamentId)
    {
        var result = await Mediator.Send(new LeaveTournamentCommand { TournamentId = tournamentId });
        return HandleResult(result);
    }

    [HttpPost("{tournamentId}/generate-bracket")]
    public async Task<IActionResult> GenerateBracket(string tournamentId)
    {
        var result = await Mediator.Send(new GenerateBracketCommand { TournamentId = tournamentId });
        return HandleResult(result);
    }

    [HttpPost("matches/{matchId}/result")]
    public async Task<IActionResult> ReportResult(string matchId, [FromBody] ReportTournamentMatchResultCommand command)
    {
        var result = await Mediator.Send(command with { MatchId = matchId });
        return HandleResult(result);
    }
}
