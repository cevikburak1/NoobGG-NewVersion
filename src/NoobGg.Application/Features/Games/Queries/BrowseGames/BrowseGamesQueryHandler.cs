using System.Text.RegularExpressions;
using MediatR;
using MongoDB.Bson;
using MongoDB.Driver;
using NoobGg.Application.Common.Constants;
using NoobGg.Application.Common.Interfaces;
using NoobGg.Application.Common.Models;
using NoobGg.Application.Features.Games.DTOs;
using NoobGg.Domain.Entities;

namespace NoobGg.Application.Features.Games.Queries.BrowseGames;

public class BrowseGamesQueryHandler : IRequestHandler<BrowseGamesQuery, Result<PagedResult<GameResponse>>>
{
    private readonly IMongoContext _mongoContext;

    public BrowseGamesQueryHandler(IMongoContext mongoContext)
    {
        _mongoContext = mongoContext;
    }

    public async Task<Result<PagedResult<GameResponse>>> Handle(BrowseGamesQuery request, CancellationToken ct)
    {
        var collection = _mongoContext.GetCollection<Game>(CollectionNames.Games);

        var filters = new List<FilterDefinition<Game>>
        {
            Builders<Game>.Filter.Eq(g => g.IsActive, true)
        };

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var term = Regex.Escape(request.Search.Trim().ToLowerInvariant());
            filters.Add(Builders<Game>.Filter.Regex(g => g.NameNormalized, new BsonRegularExpression(term)));
        }

        if (!string.IsNullOrWhiteSpace(request.Genre))
            filters.Add(Builders<Game>.Filter.AnyEq(g => g.Genres, request.Genre));

        if (!string.IsNullOrWhiteSpace(request.Platform))
            filters.Add(Builders<Game>.Filter.AnyEq(g => g.Platforms, request.Platform));

        if (request.IsMultiplayer == true)
            filters.Add(Builders<Game>.Filter.Eq(g => g.IsMultiplayer, true));

        if (request.IsCoop == true)
            filters.Add(Builders<Game>.Filter.Eq(g => g.IsCoop, true));

        if (request.IsPvp == true)
            filters.Add(Builders<Game>.Filter.Eq(g => g.IsPvp, true));

        if (request.IsFreeToPlay == true)
            filters.Add(Builders<Game>.Filter.Eq(g => g.IsFreeToPlay, true));

        var filter = Builders<Game>.Filter.And(filters);

        var totalCount = await collection.CountDocumentsAsync(filter, cancellationToken: ct);

        var skip = (request.Page - 1) * request.PageSize;
        var items = await collection.Find(filter)
            .SortByDescending(g => g.Metacritic)
            .ThenBy(g => g.NameNormalized)
            .Skip(skip)
            .Limit(request.PageSize)
            .ToListAsync(ct);

        var result = new PagedResult<GameResponse>
        {
            Items = items.Select(MapToResponse).ToList(),
            TotalCount = (int)totalCount,
            Page = request.Page,
            PageSize = request.PageSize
        };

        return Result<PagedResult<GameResponse>>.Success(result);
    }

    private static GameResponse MapToResponse(Game game) => new()
    {
        Id = game.Id,
        RawgId = game.RawgId,
        Slug = game.Slug,
        Name = game.Name,
        Description = game.Description,
        BackgroundImageUrl = game.BackgroundImageUrl,
        ReleasedAt = game.ReleasedAt,
        Rating = game.Rating,
        Metacritic = game.Metacritic,
        Genres = game.Genres,
        Tags = game.Tags,
        Platforms = game.Platforms,
        IsMultiplayer = game.IsMultiplayer,
        IsCoop = game.IsCoop,
        IsPvp = game.IsPvp,
        IsFreeToPlay = game.IsFreeToPlay
    };
}
