namespace NoobGg.Application.Features.Elo.DTOs;

public record LeaderboardEntryResponse(
    int Position,
    string UserId,
    string Username,
    string? AvatarUrl,
    int EloPoints,
    string RankTier,
    int? HoursPlayed,
    bool LookingForTeam);
