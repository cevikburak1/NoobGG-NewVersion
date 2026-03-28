using MediatR;
using NoobGg.Application.Common.Models;
using NoobGg.Application.Features.Favorites.DTOs;

namespace NoobGg.Application.Features.Favorites.Queries.GetMyFavorites;

public record GetMyFavoritesQuery : IRequest<Result<List<FavoritePlayerResponse>>>;
