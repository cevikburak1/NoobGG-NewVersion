using MediatR;
using MongoDB.Driver;
using NoobGg.Application.Common.Constants;
using NoobGg.Application.Common.Interfaces;
using NoobGg.Application.Common.Models;
using NoobGg.Application.Features.Tournaments.DTOs;
using NoobGg.Domain.Entities;

namespace NoobGg.Application.Features.Tournaments.Queries.GetTournaments;

public class GetTournamentsQueryHandler : IRequestHandler<GetTournamentsQuery, Result<TournamentListResponse>>
{
    private readonly IMongoContext _mongoContext;

    public GetTournamentsQueryHandler(IMongoContext mongoContext)
    {
        _mongoContext = mongoContext;
    }

    public async Task<Result<TournamentListResponse>> Handle(GetTournamentsQuery request, CancellationToken ct)
    {
        var tournaments = _mongoContext.GetCollection<Tournament>(CollectionNames.Tournaments);

        var filterBuilder = Builders<Tournament>.Filter;
        var filters = new List<FilterDefinition<Tournament>>();

        if (!string.IsNullOrWhiteSpace(request.GameId))
            filters.Add(filterBuilder.Eq(t => t.GameId, request.GameId));

        if (!string.IsNullOrWhiteSpace(request.GuildId))
            filters.Add(filterBuilder.Eq(t => t.GuildId, request.GuildId));

        if (request.Status.HasValue)
            filters.Add(filterBuilder.Eq(t => t.Status, request.Status.Value));

        var combinedFilter = filters.Count > 0
            ? filterBuilder.And(filters)
            : filterBuilder.Empty;

        var totalCount = await tournaments.CountDocumentsAsync(combinedFilter, cancellationToken: ct);
        var skip = (request.Page - 1) * request.PageSize;

        var tournamentDocs = await tournaments.Find(combinedFilter)
            .SortByDescending(t => t.CreatedAt)
            .Skip(skip)
            .Limit(request.PageSize)
            .ToListAsync(ct);

        var gameIds = tournamentDocs.Select(t => t.GameId).Distinct().ToList();
        var games = _mongoContext.GetCollection<Game>(CollectionNames.Games);
        var gameDocs = await games.Find(Builders<Game>.Filter.In(g => g.Id, gameIds)).ToListAsync(ct);
        var gameMap = gameDocs.ToDictionary(g => g.Id, g => g.Name);

        var organizerIds = tournamentDocs.Select(t => t.OrganizerId).Distinct().ToList();
        var users = _mongoContext.GetCollection<User>(CollectionNames.Users);
        var userDocs = await users.Find(Builders<User>.Filter.In(u => u.Id, organizerIds)).ToListAsync(ct);
        var userMap = userDocs.ToDictionary(u => u.Id, u => u.Username);

        var items = tournamentDocs.Select(t => new TournamentListItemResponse(
            t.Id, t.Name, t.Description,
            t.GameId, gameMap.GetValueOrDefault(t.GameId, "Unknown"),
            t.OrganizerId, userMap.GetValueOrDefault(t.OrganizerId, "Unknown"),
            t.GuildId,
            t.Format.ToString(), t.Status.ToString(),
            t.MaxParticipants, t.CurrentParticipants,
            t.RegistrationDeadline, t.StartsAt,
            t.PrizeBadges, t.CreatedAt
        )).ToList();

        var hasMore = skip + request.PageSize < totalCount;

        return Result<TournamentListResponse>.Success(
            new TournamentListResponse(items, (int)totalCount, hasMore));
    }
}
