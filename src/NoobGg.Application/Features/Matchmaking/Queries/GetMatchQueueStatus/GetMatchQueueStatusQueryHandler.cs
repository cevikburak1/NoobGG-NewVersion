using MediatR;
using MongoDB.Driver;
using NoobGg.Application.Common;
using NoobGg.Application.Common.Constants;
using NoobGg.Application.Common.Interfaces;
using NoobGg.Application.Common.Models;
using NoobGg.Application.Features.Matchmaking.DTOs;
using NoobGg.Domain.Entities;
using NoobGg.Domain.Enums;

namespace NoobGg.Application.Features.Matchmaking.Queries.GetMatchQueueStatus;

public class GetMatchQueueStatusQueryHandler : IRequestHandler<GetMatchQueueStatusQuery, Result<GetMatchQueueStatusResponse>>
{
    private readonly IMongoContext _mongoContext;
    private readonly ICurrentUser _currentUser;

    public GetMatchQueueStatusQueryHandler(IMongoContext mongoContext, ICurrentUser currentUser)
    {
        _mongoContext = mongoContext;
        _currentUser = currentUser;
    }

    public async Task<Result<GetMatchQueueStatusResponse>> Handle(GetMatchQueueStatusQuery request, CancellationToken ct)
    {
        if (!_currentUser.IsAuthenticated || _currentUser.UserId is null)
            return Result<GetMatchQueueStatusResponse>.Unauthorized();

        var userId = _currentUser.UserId;
        var queue = _mongoContext.GetCollection<MatchQueueEntry>(CollectionNames.MatchQueueEntries);

        var entry = await queue.Find(e =>
                e.UserId == userId &&
                (e.Status == MatchQueueEntryStatus.Searching ||
                 e.Status == MatchQueueEntryStatus.FallbackSuggested ||
                 e.Status == MatchQueueEntryStatus.Matched))
            .SortByDescending(e => e.UpdatedAt)
            .FirstOrDefaultAsync(ct);

        if (entry is null)
            return Result<GetMatchQueueStatusResponse>.Success(
                new GetMatchQueueStatusResponse("Idle", null, false, null, null));

        if (entry.ExpiresAt < DateTime.UtcNow &&
            entry.Status is MatchQueueEntryStatus.Searching or MatchQueueEntryStatus.FallbackSuggested)
        {
            await queue.UpdateOneAsync(
                e => e.Id == entry.Id,
                Builders<MatchQueueEntry>.Update
                    .Set(e => e.Status, MatchQueueEntryStatus.Expired)
                    .Set(e => e.UpdatedAt, DateTime.UtcNow),
                cancellationToken: ct);
            return Result<GetMatchQueueStatusResponse>.Success(
                new GetMatchQueueStatusResponse("Idle", null, false, null, null));
        }

        if (entry.Status == MatchQueueEntryStatus.Matched && !string.IsNullOrEmpty(entry.MatchedRoomId))
            return Result<GetMatchQueueStatusResponse>.Success(
                new GetMatchQueueStatusResponse(
                    "Matched",
                    entry.MatchedRoomId,
                    false,
                    entry.GameId,
                    (int)(DateTime.UtcNow - entry.CreatedAt).TotalSeconds));

        var secondsInQueue = (int)(DateTime.UtcNow - entry.CreatedAt).TotalSeconds;
        var fallbackReady = secondsInQueue >= MatchmakingConstants.FallbackAfterSeconds;

        if (fallbackReady && entry.Status == MatchQueueEntryStatus.Searching)
        {
            await queue.UpdateOneAsync(
                e => e.Id == entry.Id && e.Status == MatchQueueEntryStatus.Searching,
                Builders<MatchQueueEntry>.Update
                    .Set(e => e.Status, MatchQueueEntryStatus.FallbackSuggested)
                    .Set(e => e.UpdatedAt, DateTime.UtcNow),
                cancellationToken: ct);
            entry.Status = MatchQueueEntryStatus.FallbackSuggested;
        }

        var statusLabel = entry.Status == MatchQueueEntryStatus.FallbackSuggested || fallbackReady
            ? "FallbackSuggested"
            : "Searching";

        return Result<GetMatchQueueStatusResponse>.Success(
            new GetMatchQueueStatusResponse(
                statusLabel,
                null,
                fallbackReady || entry.Status == MatchQueueEntryStatus.FallbackSuggested,
                entry.GameId,
                secondsInQueue));
    }
}
