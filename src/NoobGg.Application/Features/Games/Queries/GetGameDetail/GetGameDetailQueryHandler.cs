using MediatR;
using MongoDB.Driver;
using NoobGg.Application.Common.Constants;
using NoobGg.Application.Common.Interfaces;
using NoobGg.Application.Common.Models;
using NoobGg.Application.Features.Games.DTOs;
using NoobGg.Domain.Entities;

namespace NoobGg.Application.Features.Games.Queries.GetGameDetail;

public class GetGameDetailQueryHandler : IRequestHandler<GetGameDetailQuery, Result<GameResponse>>
{
    private readonly IMongoContext _mongoContext;

    public GetGameDetailQueryHandler(IMongoContext mongoContext)
    {
        _mongoContext = mongoContext;
    }

    public async Task<Result<GameResponse>> Handle(GetGameDetailQuery request, CancellationToken ct)
    {
        var collection = _mongoContext.GetCollection<Game>(CollectionNames.Games);

        var game = await collection
            .Find(g => g.Id == request.GameId && g.IsActive)
            .FirstOrDefaultAsync(ct);

        if (game is null)
            return Result<GameResponse>.Fail("Game not found", 404);

        return Result<GameResponse>.Success(new GameResponse
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
        });
    }
}
