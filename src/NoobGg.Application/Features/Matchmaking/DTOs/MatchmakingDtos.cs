namespace NoobGg.Application.Features.Matchmaking.DTOs;

public record JoinMatchQueueResponse(string Status, string? MatchedRoomId, bool FallbackReady);

public record GetMatchQueueStatusResponse(
    string Status,
    string? MatchedRoomId,
    bool FallbackReady,
    string? GameId,
    int? SecondsInQueue);
