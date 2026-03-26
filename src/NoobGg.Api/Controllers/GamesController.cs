using Microsoft.AspNetCore.Mvc;
using NoobGg.Application.Features.Games.Queries.BrowseGames;
using NoobGg.Application.Features.Games.Queries.GetGameDetail;
using NoobGg.Application.Features.Games.Queries.SearchGames;

namespace NoobGg.Api.Controllers;

[Route("api/games")]
public class GamesController : ApiControllerBase
{
    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(string id)
    {
        var result = await Mediator.Send(new GetGameDetailQuery { GameId = id });
        return HandleResult(result);
    }

    [HttpGet]
    public async Task<IActionResult> Browse(
        [FromQuery] string? search = null,
        [FromQuery] string? genre = null,
        [FromQuery] string? platform = null,
        [FromQuery] bool? multiplayer = null,
        [FromQuery] bool? coop = null,
        [FromQuery] bool? pvp = null,
        [FromQuery] bool? freeToPlay = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 12)
    {
        var query = new BrowseGamesQuery
        {
            Search = search,
            Genre = genre,
            Platform = platform,
            IsMultiplayer = multiplayer,
            IsCoop = coop,
            IsPvp = pvp,
            IsFreeToPlay = freeToPlay,
            Page = page,
            PageSize = pageSize
        };

        var result = await Mediator.Send(query);
        return HandleResult(result);
    }

    [HttpGet("search")]
    public async Task<IActionResult> Search(
        [FromQuery(Name = "q")] string searchTerm,
        [FromQuery] int limit = 10,
        [FromQuery] bool? multiplayer = null,
        [FromQuery] bool? coop = null,
        [FromQuery] string? genre = null)
    {
        var query = new SearchGamesQuery
        {
            SearchTerm = searchTerm,
            Limit = limit,
            IsMultiplayer = multiplayer,
            IsCoop = coop,
            Genre = genre
        };

        var result = await Mediator.Send(query);
        return HandleResult(result);
    }
}
