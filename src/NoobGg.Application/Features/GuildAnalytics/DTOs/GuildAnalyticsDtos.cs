namespace NoobGg.Application.Features.GuildAnalytics.DTOs;

public record GuildTopPlayerResponse(
    string UserId,
    string Username,
    string? AvatarUrl,
    int EloPoints,
    string RankTier,
    int TotalMatches,
    int Wins,
    double WinRate);

public record GuildStatsResponse(
    string GuildId,
    string GuildName,
    int TotalMembers,
    int TotalMatches,
    int TotalWins,
    double OverallWinRate,
    List<GuildTopPlayerResponse> TopPlayers,
    List<GuildActivityPoint> ActivityTimeline);

public record GuildActivityPoint(string Date, int MatchesPlayed, int MembersJoined);
