using MediatR;
using MongoDB.Driver;
using NoobGg.Application.Common.Constants;
using NoobGg.Application.Common.Interfaces;
using NoobGg.Application.Common.Models;
using NoobGg.Application.Features.Rooms.DTOs;
using NoobGg.Domain.Entities;
using NoobGg.Domain.Enums;

namespace NoobGg.Application.Features.Rooms.Queries.GetRooms;

public class GetRoomsQueryHandler : IRequestHandler<GetRoomsQuery, Result<PagedResult<RoomResponse>>>
{
    private readonly IMongoContext _mongoContext;

    public GetRoomsQueryHandler(IMongoContext mongoContext)
    {
        _mongoContext = mongoContext;
    }

    public async Task<Result<PagedResult<RoomResponse>>> Handle(GetRoomsQuery request, CancellationToken ct)
    {
        var rooms = _mongoContext.GetCollection<Room>(CollectionNames.Rooms);

        var filterBuilder = Builders<Room>.Filter;
        var filters = new List<FilterDefinition<Room>>();

        // Default to showing only public, open rooms unless explicitly filtered
        var status = request.Status ?? RoomStatus.Open;
        filters.Add(filterBuilder.Eq(r => r.Status, status));
        filters.Add(filterBuilder.Eq(r => r.IsPublic, true));

        if (!string.IsNullOrWhiteSpace(request.GameId))
            filters.Add(filterBuilder.Eq(r => r.GameId, request.GameId));

        if (request.Region.HasValue)
            filters.Add(filterBuilder.Eq(r => r.Region, request.Region.Value));

        if (request.Language.HasValue)
            filters.Add(filterBuilder.Eq(r => r.Language, request.Language.Value));

        if (!string.IsNullOrWhiteSpace(request.Tag))
            filters.Add(filterBuilder.AnyEq(r => r.Tags, request.Tag.Trim().ToLowerInvariant()));

        var combinedFilter = filterBuilder.And(filters);

        var totalCount = await rooms.CountDocumentsAsync(combinedFilter, cancellationToken: ct);

        var skip = (request.Page - 1) * request.PageSize;

        var roomDocs = await rooms.Find(combinedFilter)
            .SortByDescending(r => r.CreatedAt)
            .Skip(skip)
            .Limit(request.PageSize)
            .ToListAsync(ct);

        var gameIds = roomDocs.Select(r => r.GameId).Distinct().ToList();
        var games = _mongoContext.GetCollection<Game>(CollectionNames.Games);
        var gameDocs = await games.Find(Builders<Game>.Filter.In(g => g.Id, gameIds)).ToListAsync(ct);
        var gameMap = gameDocs.ToDictionary(g => g.Id, g => (g.Name, g.BackgroundImageUrl));

        var items = roomDocs.Select(r =>
        {
            gameMap.TryGetValue(r.GameId, out var game);
            return new RoomResponse(
                r.Id,
                r.Title,
                r.GameId,
                game.Name,
                game.BackgroundImageUrl,
                r.CreatorId,
                r.IsPublic,
                r.MaxMembers,
                r.CurrentMemberCount,
                r.Region.ToString(),
                r.Language.ToString(),
                r.Tags,
                r.Status.ToString(),
                r.CreatedAt);
        }).ToList();

        var result = new PagedResult<RoomResponse>
        {
            Items = items,
            TotalCount = (int)totalCount,
            Page = request.Page,
            PageSize = request.PageSize
        };

        return Result<PagedResult<RoomResponse>>.Success(result);
    }
}
