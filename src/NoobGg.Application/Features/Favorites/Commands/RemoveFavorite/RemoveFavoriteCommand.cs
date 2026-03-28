using MediatR;
using NoobGg.Application.Common.Models;

namespace NoobGg.Application.Features.Favorites.Commands.RemoveFavorite;

public record RemoveFavoriteCommand : IRequest<Result>
{
    public string FavoriteUserId { get; init; } = string.Empty;
}
