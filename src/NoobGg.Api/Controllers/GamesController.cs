using Microsoft.AspNetCore.Mvc;
using NoobGg.Application.Features.Games.Queries.SearchGames;

namespace NoobGg.Api.Controllers;

[Route("api/games")]
public class GamesController : ApiControllerBase
{
    /// <summary>
    /// Search games for frontend autocomplete.
    /// GET /api/games/search?q=counter&amp;limit=10&amp;multiplayer=true&amp;genre=Action
    /// </summary>
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
