using MediatR;
using MongoDB.Driver;
using NoobGg.Application.Common.Constants;
using NoobGg.Application.Common.Interfaces;
using NoobGg.Application.Common.Models;
using NoobGg.Application.Features.GuildEvents.DTOs;
using NoobGg.Domain.Entities;

namespace NoobGg.Application.Features.GuildEvents.Queries.GetEvents;

public class GetGuildEventsQueryHandler : IRequestHandler<GetGuildEventsQuery, Result<GuildEventListResponse>>
{
    private readonly IMongoContext _mongoContext;
    private readonly ICurrentUser _currentUser;

    public GetGuildEventsQueryHandler(IMongoContext mongoContext, ICurrentUser currentUser)
    {
        _mongoContext = mongoContext;
        _currentUser = currentUser;
    }

    public async Task<Result<GuildEventListResponse>> Handle(GetGuildEventsQuery request, CancellationToken ct)
    {
        if (!_currentUser.IsAuthenticated || _currentUser.UserId is null)
            return Result<GuildEventListResponse>.Unauthorized();

        var events = _mongoContext.GetCollection<GuildEvent>(CollectionNames.GuildEvents);

        var filter = Builders<GuildEvent>.Filter.Eq(e => e.GuildId, request.GuildId);

        if (request.From.HasValue)
            filter = Builders<GuildEvent>.Filter.And(
                filter,
                Builders<GuildEvent>.Filter.Gte(e => e.StartsAt, request.From.Value));

        if (request.To.HasValue)
            filter = Builders<GuildEvent>.Filter.And(
                filter,
                Builders<GuildEvent>.Filter.Lte(e => e.StartsAt, request.To.Value));

        var totalCount = await events.CountDocumentsAsync(filter, cancellationToken: ct);

        var eventList = await events
            .Find(filter)
            .SortBy(e => e.StartsAt)
            .ToListAsync(ct);

        var creatorIds = eventList.Select(e => e.CreatorId).Distinct().ToList();
        var users = _mongoContext.GetCollection<User>(CollectionNames.Users);
        var creatorList = await users
            .Find(Builders<User>.Filter.In(u => u.Id, creatorIds))
            .ToListAsync(ct);
        var creatorLookup = creatorList.ToDictionary(u => u.Id, u => u.Username);

        var responses = eventList.Select(e => new GuildEventResponse(
            e.Id, e.GuildId, e.CreatorId,
            creatorLookup.GetValueOrDefault(e.CreatorId, "Unknown"),
            e.Title, e.Description,
            e.StartsAt, e.EndsAt,
            e.GameId, e.TournamentId,
            e.CreatedAt)).ToList();

        return Result<GuildEventListResponse>.Success(
            new GuildEventListResponse(responses, (int)totalCount));
    }
}
