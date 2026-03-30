namespace NoobGg.Application.Features.Elo.DTOs;

public record EloHistoryResponse(
    int CurrentElo,
    string RankTier,
    string GameId,
    string GameName,
    List<EloSnapshotDto> History,
    List<RecentMatchDto> RecentMatches);

public record EloSnapshotDto(int Points, DateTime RecordedAt);

public record RecentMatchDto(
    string MatchId,
    string OpponentId,
    string OpponentUsername,
    bool Won,
    int EloChange,
    int EloBefore,
    DateTime PlayedAt);
