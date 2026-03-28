using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NoobGg.Application.Features.Favorites.Commands.AddFavorite;
using NoobGg.Application.Features.Favorites.Commands.RemoveFavorite;
using NoobGg.Application.Features.Favorites.Queries.GetMyFavorites;

namespace NoobGg.Api.Controllers;

[Route("api/favorites")]
[Authorize]
public class FavoritesController : ApiControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetMyFavorites()
    {
        var result = await Mediator.Send(new GetMyFavoritesQuery());
        return HandleResult(result);
    }

    [HttpPost("{userId}")]
    public async Task<IActionResult> AddFavorite(string userId)
    {
        var result = await Mediator.Send(new AddFavoriteCommand { FavoriteUserId = userId });
        return HandleResult(result);
    }

    [HttpDelete("{userId}")]
    public async Task<IActionResult> RemoveFavorite(string userId)
    {
        var result = await Mediator.Send(new RemoveFavoriteCommand { FavoriteUserId = userId });
        return HandleResult(result);
    }
}
