namespace NoobGg.Application.Features.Favorites.DTOs;

public record FavoritePlayerResponse(
    string UserId,
    string Username,
    string? AvatarUrl,
    bool IsOnline,
    DateTime FavoritedAt);
