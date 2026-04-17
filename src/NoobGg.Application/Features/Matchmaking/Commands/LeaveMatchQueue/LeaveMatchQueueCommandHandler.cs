using MediatR;
using MongoDB.Driver;
using NoobGg.Application.Common.Constants;
using NoobGg.Application.Common.Interfaces;
using NoobGg.Application.Common.Models;
using NoobGg.Domain.Entities;
using NoobGg.Domain.Enums;

namespace NoobGg.Application.Features.Matchmaking.Commands.LeaveMatchQueue;

public class LeaveMatchQueueCommandHandler : IRequestHandler<LeaveMatchQueueCommand, Result>
{
    private readonly IMongoContext _mongoContext;
    private readonly ICurrentUser _currentUser;

    public LeaveMatchQueueCommandHandler(IMongoContext mongoContext, ICurrentUser currentUser)
    {
        _mongoContext = mongoContext;
        _currentUser = currentUser;
    }

    public async Task<Result> Handle(LeaveMatchQueueCommand request, CancellationToken ct)
    {
        if (!_currentUser.IsAuthenticated || _currentUser.UserId is null)
            return Result.Fail("Unauthorized", 401);

        var userId = _currentUser.UserId;
        var queue = _mongoContext.GetCollection<MatchQueueEntry>(CollectionNames.MatchQueueEntries);

        await queue.UpdateManyAsync(
            e => e.UserId == userId &&
                 (e.Status == MatchQueueEntryStatus.Searching ||
                  e.Status == MatchQueueEntryStatus.FallbackSuggested ||
                  e.Status == MatchQueueEntryStatus.Matched),
            Builders<MatchQueueEntry>.Update
                .Set(e => e.Status, MatchQueueEntryStatus.Cancelled)
                .Set(e => e.UpdatedAt, DateTime.UtcNow),
            cancellationToken: ct);

        return Result.Success();
    }
}
