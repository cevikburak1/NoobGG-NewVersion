using MediatR;
using NoobGg.Application.Common.Models;

namespace NoobGg.Application.Features.Favorites.Commands.AddFavorite;

public record AddFavoriteCommand : IRequest<Result>
{
    public string FavoriteUserId { get; init; } = string.Empty;
}
