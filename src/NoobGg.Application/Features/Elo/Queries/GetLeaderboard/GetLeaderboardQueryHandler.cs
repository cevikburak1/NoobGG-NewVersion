using MediatR;
using MongoDB.Driver;
using NoobGg.Application.Common.Constants;
using NoobGg.Application.Common.Interfaces;
using NoobGg.Application.Common.Models;
using NoobGg.Application.Features.Elo.DTOs;
using NoobGg.Domain.Entities;

namespace NoobGg.Application.Features.Elo.Queries.GetLeaderboard;

public class GetLeaderboardQueryHandler
    : IRequestHandler<GetLeaderboardQuery, Result<PagedResult<LeaderboardEntryResponse>>>
{
    private readonly IMongoContext _mongoContext;

    public GetLeaderboardQueryHandler(IMongoContext mongoContext)
    {
        _mongoContext = mongoContext;
    }

    public async Task<Result<PagedResult<LeaderboardEntryResponse>>> Handle(
        GetLeaderboardQuery request, CancellationToken ct)
    {
        var gpCol = _mongoContext.GetCollection<UserGameProfile>(CollectionNames.UserGameProfiles);
        var usersCol = _mongoContext.GetCollection<User>(CollectionNames.Users);
        var profilesCol = _mongoContext.GetCollection<UserProfile>(CollectionNames.UserProfiles);

        var filter = Builders<UserGameProfile>.Filter.Eq(p => p.GameId, request.GameId);
        var totalCount = (int)await gpCol.CountDocumentsAsync(filter, cancellationToken: ct);

        var skip = (request.Page - 1) * request.PageSize;

        var gameProfiles = await gpCol.Find(filter)
            .SortByDescending(p => p.EloPoints)
            .Skip(skip)
            .Limit(request.PageSize)
            .ToListAsync(ct);

        if (gameProfiles.Count == 0)
            return Result<PagedResult<LeaderboardEntryResponse>>.Success(
                new PagedResult<LeaderboardEntryResponse>
                {
                    Items = [],
                    TotalCount = 0,
                    Page = request.Page,
                    PageSize = request.PageSize
                });

        var userIds = gameProfiles.Select(p => p.UserId).ToList();
        var userDocs = await usersCol.Find(Builders<User>.Filter.In(u => u.Id, userIds)).ToListAsync(ct);
        var usernameMap = userDocs.ToDictionary(u => u.Id, u => u.Username);

        var userProfiles = await profilesCol
            .Find(Builders<UserProfile>.Filter.In(p => p.UserId, userIds))
            .ToListAsync(ct);
        var avatarMap = userProfiles.ToDictionary(p => p.UserId, p => p.AvatarUrl);

        var entries = gameProfiles.Select((gp, idx) => new LeaderboardEntryResponse(
            skip + idx + 1,
            gp.UserId,
            usernameMap.GetValueOrDefault(gp.UserId, "Unknown"),
            avatarMap.GetValueOrDefault(gp.UserId),
            gp.EloPoints,
            gp.RankTier.ToString(),
            gp.HoursPlayed,
            gp.LookingForTeam)).ToList();

        return Result<PagedResult<LeaderboardEntryResponse>>.Success(
            new PagedResult<LeaderboardEntryResponse>
            {
                Items = entries,
                TotalCount = totalCount,
                Page = request.Page,
                PageSize = request.PageSize
            });
    }
}
