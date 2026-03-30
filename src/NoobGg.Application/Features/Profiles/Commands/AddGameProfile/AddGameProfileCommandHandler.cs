using MediatR;
using MongoDB.Driver;
using NoobGg.Application.Common.Constants;
using NoobGg.Application.Common.Interfaces;
using NoobGg.Application.Common.Models;
using NoobGg.Application.Features.Profiles.DTOs;
using NoobGg.Domain.Entities;

namespace NoobGg.Application.Features.Profiles.Commands.AddGameProfile;

public class AddGameProfileCommandHandler : IRequestHandler<AddGameProfileCommand, Result<GameProfileResponse>>
{
    private readonly IMongoContext _mongoContext;
    private readonly ICurrentUser _currentUser;

    public AddGameProfileCommandHandler(IMongoContext mongoContext, ICurrentUser currentUser)
    {
        _mongoContext = mongoContext;
        _currentUser = currentUser;
    }

    public async Task<Result<GameProfileResponse>> Handle(AddGameProfileCommand request, CancellationToken ct)
    {
        if (!_currentUser.IsAuthenticated || _currentUser.UserId is null)
            return Result<GameProfileResponse>.Unauthorized();

        var userId = _currentUser.UserId;
        var gameProfiles = _mongoContext.GetCollection<UserGameProfile>(CollectionNames.UserGameProfiles);
        var games = _mongoContext.GetCollection<Game>(CollectionNames.Games);

        var game = await games.Find(g => g.Id == request.GameId).FirstOrDefaultAsync(ct);
        if (game is null)
            return Result<GameProfileResponse>.NotFound("Game not found");

        var existing = await gameProfiles.Find(
            gp => gp.UserId == userId && gp.GameId == request.GameId
        ).AnyAsync(ct);

        if (existing)
            return Result<GameProfileResponse>.Fail("You already have a profile for this game");

        var gp = new UserGameProfile
        {
            UserId = userId,
            GameId = request.GameId,
            Rank = request.Rank,
            Role = request.Role,
            Region = request.Region,
            ExperienceLevel = request.ExperienceLevel,
            CommunicationPreference = request.CommunicationPreference,
            HoursPlayed = request.HoursPlayed,
            LookingForTeam = request.LookingForTeam,
            Note = request.Note
        };

        await gameProfiles.InsertOneAsync(gp, cancellationToken: ct);

        return Result<GameProfileResponse>.Created(new GameProfileResponse
        {
            Id = gp.Id,
            UserId = gp.UserId,
            GameId = gp.GameId,
            GameName = game.Name,
            GameImageUrl = game.BackgroundImageUrl,
            Rank = gp.Rank,
            Role = gp.Role,
            Region = gp.Region.ToString(),
            ExperienceLevel = gp.ExperienceLevel.ToString(),
            CommunicationPreference = gp.CommunicationPreference.ToString(),
            HoursPlayed = gp.HoursPlayed,
            LookingForTeam = gp.LookingForTeam,
            Note = gp.Note,
            InGameName = request.InGameName,
            EloPoints = gp.EloPoints,
            RankTier = gp.RankTier.ToString()
        });
    }
}
