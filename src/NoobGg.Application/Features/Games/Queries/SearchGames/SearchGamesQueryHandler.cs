using System.Text.RegularExpressions;
using MediatR;
using MongoDB.Bson;
using MongoDB.Driver;
using NoobGg.Application.Common.Constants;
using NoobGg.Application.Common.Interfaces;
using NoobGg.Application.Common.Models;
using NoobGg.Application.Features.Games.DTOs;
using NoobGg.Domain.Entities;

namespace NoobGg.Application.Features.Games.Queries.SearchGames;

public class SearchGamesQueryHandler : IRequestHandler<SearchGamesQuery, Result<List<GameResponse>>>
{
    private readonly IMongoContext _mongoContext;
    private readonly ICacheService _cacheService;

    private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(5);

    public SearchGamesQueryHandler(IMongoContext mongoContext, ICacheService cacheService)
    {
        _mongoContext = mongoContext;
        _cacheService = cacheService;
    }

    public async Task<Result<List<GameResponse>>> Handle(SearchGamesQuery request, CancellationToken ct)
    {
        var normalizedTerm = request.SearchTerm.Trim().ToLowerInvariant();
        var cacheKey = BuildCacheKey(normalizedTerm, request);

        var cached = await _cacheService.GetAsync<List<GameResponse>>(cacheKey, ct);
        if (cached is not null)
            return Result<List<GameResponse>>.Success(cached);

        var games = _mongoContext.GetCollection<Game>(CollectionNames.Games);

        var filters = new List<FilterDefinition<Game>>
        {
            Builders<Game>.Filter.Eq(g => g.IsActive, true),
            Builders<Game>.Filter.Regex(g => g.NameNormalized, new BsonRegularExpression(Regex.Escape(normalizedTerm)))
        };

        if (request.IsMultiplayer.HasValue)
            filters.Add(Builders<Game>.Filter.Eq(g => g.IsMultiplayer, request.IsMultiplayer.Value));

        if (request.IsCoop.HasValue)
            filters.Add(Builders<Game>.Filter.Eq(g => g.IsCoop, request.IsCoop.Value));

        if (!string.IsNullOrWhiteSpace(request.Genre))
            filters.Add(Builders<Game>.Filter.AnyEq(g => g.Genres, request.Genre));

        var filter = Builders<Game>.Filter.And(filters);

        var candidates = await games.Find(filter)
            .SortBy(g => g.NameNormalized)
            .Limit(request.Limit * 3)
            .ToListAsync(ct);

        var results = candidates
            .OrderByDescending(g => g.NameNormalized.StartsWith(normalizedTerm))
            .ThenBy(g => g.NameNormalized.Length)
            .ThenBy(g => g.NameNormalized)
            .Take(request.Limit)
            .Select(MapToResponse)
            .ToList();

        await _cacheService.SetAsync(cacheKey, results, CacheDuration, ct);

        return Result<List<GameResponse>>.Success(results);
    }

    private static string BuildCacheKey(string term, SearchGamesQuery request)
    {
        var parts = new List<string> { "games", "search", term, request.Limit.ToString() };

        if (request.IsMultiplayer.HasValue)
            parts.Add($"mp:{request.IsMultiplayer.Value}");
        if (request.IsCoop.HasValue)
            parts.Add($"coop:{request.IsCoop.Value}");
        if (!string.IsNullOrWhiteSpace(request.Genre))
            parts.Add($"g:{request.Genre.ToLowerInvariant()}");

        return string.Join(":", parts);
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
