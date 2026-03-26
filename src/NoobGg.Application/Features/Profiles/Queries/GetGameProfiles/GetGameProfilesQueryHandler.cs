using MediatR;
using MongoDB.Driver;
using NoobGg.Application.Common.Constants;
using NoobGg.Application.Common.Interfaces;
using NoobGg.Application.Common.Models;
using NoobGg.Application.Features.Profiles.DTOs;
using NoobGg.Domain.Entities;

namespace NoobGg.Application.Features.Profiles.Queries.GetGameProfiles;

public class GetGameProfilesQueryHandler : IRequestHandler<GetGameProfilesQuery, Result<List<GameProfileResponse>>>
{
    private readonly IMongoContext _mongoContext;

    public GetGameProfilesQueryHandler(IMongoContext mongoContext)
    {
        _mongoContext = mongoContext;
    }

    public async Task<Result<List<GameProfileResponse>>> Handle(GetGameProfilesQuery request, CancellationToken ct)
    {
        var gameProfiles = _mongoContext.GetCollection<UserGameProfile>(CollectionNames.UserGameProfiles);
        var games = _mongoContext.GetCollection<Game>(CollectionNames.Games);

        var gpList = await gameProfiles.Find(gp => gp.UserId == request.UserId).ToListAsync(ct);

        if (gpList.Count == 0)
            return Result<List<GameProfileResponse>>.Success([]);

        var gameIds = gpList.Select(gp => gp.GameId).Distinct().ToList();
        var gameList = await games.Find(Builders<Game>.Filter.In(g => g.Id, gameIds)).ToListAsync(ct);
        var gameMap = gameList.ToDictionary(g => g.Id);

        var responses = gpList.Select(gp =>
        {
            gameMap.TryGetValue(gp.GameId, out var game);
            return new GameProfileResponse
            {
                Id = gp.Id,
                UserId = gp.UserId,
                GameId = gp.GameId,
                GameName = game?.Name ?? "Unknown",
                GameImageUrl = game?.BackgroundImageUrl,
                Rank = gp.Rank,
                Role = gp.Role,
                Region = gp.Region.ToString(),
                ExperienceLevel = gp.ExperienceLevel.ToString(),
                CommunicationPreference = gp.CommunicationPreference.ToString(),
                HoursPlayed = gp.HoursPlayed,
                LookingForTeam = gp.LookingForTeam,
                Note = gp.Note
            };
        }).ToList();

        return Result<List<GameProfileResponse>>.Success(responses);
    }
}
