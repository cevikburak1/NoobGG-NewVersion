using MediatR;
using MongoDB.Driver;
using NoobGg.Application.Common.Constants;
using NoobGg.Application.Common.Interfaces;
using NoobGg.Application.Common.Models;
using NoobGg.Application.Features.Profiles.DTOs;
using NoobGg.Domain.Entities;

namespace NoobGg.Application.Features.Profiles.Commands.UpdateGameProfile;

public class UpdateGameProfileCommandHandler : IRequestHandler<UpdateGameProfileCommand, Result<GameProfileResponse>>
{
    private readonly IMongoContext _mongoContext;
    private readonly ICurrentUser _currentUser;

    public UpdateGameProfileCommandHandler(IMongoContext mongoContext, ICurrentUser currentUser)
    {
        _mongoContext = mongoContext;
        _currentUser = currentUser;
    }

    public async Task<Result<GameProfileResponse>> Handle(UpdateGameProfileCommand request, CancellationToken ct)
    {
        if (!_currentUser.IsAuthenticated || _currentUser.UserId is null)
            return Result<GameProfileResponse>.Unauthorized();

        var userId = _currentUser.UserId;
        var gameProfiles = _mongoContext.GetCollection<UserGameProfile>(CollectionNames.UserGameProfiles);
        var games = _mongoContext.GetCollection<Game>(CollectionNames.Games);

        var gp = await gameProfiles.Find(g => g.Id == request.Id && g.UserId == userId).FirstOrDefaultAsync(ct);
        if (gp is null)
            return Result<GameProfileResponse>.NotFound("Game profile not found");

        var updates = new List<UpdateDefinition<UserGameProfile>>();

        if (request.Rank is not null) updates.Add(Builders<UserGameProfile>.Update.Set(g => g.Rank, request.Rank));
        if (request.Role is not null) updates.Add(Builders<UserGameProfile>.Update.Set(g => g.Role, request.Role));
        if (request.Region.HasValue) updates.Add(Builders<UserGameProfile>.Update.Set(g => g.Region, request.Region.Value));
        if (request.ExperienceLevel.HasValue) updates.Add(Builders<UserGameProfile>.Update.Set(g => g.ExperienceLevel, request.ExperienceLevel.Value));
        if (request.CommunicationPreference.HasValue) updates.Add(Builders<UserGameProfile>.Update.Set(g => g.CommunicationPreference, request.CommunicationPreference.Value));
        if (request.HoursPlayed.HasValue) updates.Add(Builders<UserGameProfile>.Update.Set(g => g.HoursPlayed, request.HoursPlayed));
        if (request.LookingForTeam.HasValue) updates.Add(Builders<UserGameProfile>.Update.Set(g => g.LookingForTeam, request.LookingForTeam.Value));
        if (request.Note is not null) updates.Add(Builders<UserGameProfile>.Update.Set(g => g.Note, request.Note));

        updates.Add(Builders<UserGameProfile>.Update.Set(g => g.UpdatedAt, DateTime.UtcNow));

        await gameProfiles.UpdateOneAsync(
            g => g.Id == request.Id,
            Builders<UserGameProfile>.Update.Combine(updates),
            cancellationToken: ct);

        var updated = await gameProfiles.Find(g => g.Id == request.Id).FirstOrDefaultAsync(ct);
        var game = await games.Find(g => g.Id == updated.GameId).FirstOrDefaultAsync(ct);

        return Result<GameProfileResponse>.Success(new GameProfileResponse
        {
            Id = updated.Id,
            UserId = updated.UserId,
            GameId = updated.GameId,
            GameName = game?.Name ?? "Unknown",
            GameImageUrl = game?.BackgroundImageUrl,
            Rank = updated.Rank,
            Role = updated.Role,
            Region = updated.Region.ToString(),
            ExperienceLevel = updated.ExperienceLevel.ToString(),
            CommunicationPreference = updated.CommunicationPreference.ToString(),
            HoursPlayed = updated.HoursPlayed,
            LookingForTeam = updated.LookingForTeam,
            Note = updated.Note
        });
    }
}
