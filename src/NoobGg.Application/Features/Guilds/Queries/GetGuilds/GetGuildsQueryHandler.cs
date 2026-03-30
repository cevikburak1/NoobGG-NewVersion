using MediatR;
using MongoDB.Driver;
using NoobGg.Application.Common.Constants;
using NoobGg.Application.Common.Interfaces;
using NoobGg.Application.Common.Models;
using NoobGg.Application.Features.Guilds.DTOs;
using NoobGg.Domain.Entities;

namespace NoobGg.Application.Features.Guilds.Queries.GetGuilds;

public class GetGuildsQueryHandler : IRequestHandler<GetGuildsQuery, Result<PagedResult<GuildResponse>>>
{
    private readonly IMongoContext _mongoContext;

    public GetGuildsQueryHandler(IMongoContext mongoContext)
    {
        _mongoContext = mongoContext;
    }

    public async Task<Result<PagedResult<GuildResponse>>> Handle(GetGuildsQuery request, CancellationToken ct)
    {
        var guilds = _mongoContext.GetCollection<Guild>(CollectionNames.Guilds);

        var filterBuilder = Builders<Guild>.Filter;
        var filters = new List<FilterDefinition<Guild>>();

        filters.Add(filterBuilder.Eq(g => g.IsPublic, true));

        if (!string.IsNullOrWhiteSpace(request.GameId))
            filters.Add(filterBuilder.AnyEq(g => g.GameIds, request.GameId));

        if (request.Region.HasValue)
            filters.Add(filterBuilder.Eq(g => g.Region, request.Region.Value));

        if (request.Language.HasValue)
            filters.Add(filterBuilder.Eq(g => g.Language, request.Language.Value));

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var searchLower = request.Search.Trim().ToLowerInvariant();
            filters.Add(filterBuilder.Or(
                filterBuilder.Regex(g => g.Name, new MongoDB.Bson.BsonRegularExpression(searchLower, "i")),
                filterBuilder.Regex(g => g.Tag, new MongoDB.Bson.BsonRegularExpression(searchLower, "i"))
            ));
        }

        var combinedFilter = filterBuilder.And(filters);

        var totalCount = await guilds.CountDocumentsAsync(combinedFilter, cancellationToken: ct);
        var skip = (request.Page - 1) * request.PageSize;

        var guildDocs = await guilds.Find(combinedFilter)
            .SortByDescending(g => g.CurrentMemberCount)
            .ThenByDescending(g => g.CreatedAt)
            .Skip(skip)
            .Limit(request.PageSize)
            .ToListAsync(ct);

        var allGameIds = guildDocs.SelectMany(g => g.GameIds).Distinct().ToList();
        var games = _mongoContext.GetCollection<Game>(CollectionNames.Games);
        var gameDocs = await games.Find(Builders<Game>.Filter.In(g => g.Id, allGameIds)).ToListAsync(ct);
        var gameMap = gameDocs.ToDictionary(g => g.Id, g => g.Name);

        var items = guildDocs.Select(g =>
        {
            var gameNames = g.GameIds
                .Where(id => gameMap.ContainsKey(id))
                .Select(id => gameMap[id])
                .ToList();

            return new GuildResponse(
                g.Id,
                g.Name,
                g.Tag,
                g.Description,
                g.CreatorId,
                g.IsPublic,
                g.MaxMembers,
                g.CurrentMemberCount,
                g.Region.ToString(),
                g.Language.ToString(),
                g.GameIds,
                gameNames,
                g.CreatedAt);
        }).ToList();

        var result = new PagedResult<GuildResponse>
        {
            Items = items,
            TotalCount = (int)totalCount,
            Page = request.Page,
            PageSize = request.PageSize
        };

        return Result<PagedResult<GuildResponse>>.Success(result);
    }
}
