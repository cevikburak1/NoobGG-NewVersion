using MediatR;
using MongoDB.Driver;
using NoobGg.Application.Common.Constants;
using NoobGg.Application.Common.Interfaces;
using NoobGg.Application.Common.Models;
using NoobGg.Application.Features.Settings.DTOs;
using NoobGg.Application.Features.Settings.Queries.GetMySettings;
using NoobGg.Domain.Entities;

namespace NoobGg.Application.Features.Settings.Commands.UpdatePrivacySettings;

public class UpdatePrivacySettingsCommandHandler
    : IRequestHandler<UpdatePrivacySettingsCommand, Result<UserSettingsResponse>>
{
    private readonly IMongoContext _mongoContext;
    private readonly ICurrentUser _currentUser;

    public UpdatePrivacySettingsCommandHandler(IMongoContext mongoContext, ICurrentUser currentUser)
    {
        _mongoContext = mongoContext;
        _currentUser = currentUser;
    }

    public async Task<Result<UserSettingsResponse>> Handle(UpdatePrivacySettingsCommand request, CancellationToken ct)
    {
        if (!_currentUser.IsAuthenticated || _currentUser.UserId is null)
            return Result<UserSettingsResponse>.Unauthorized();

        var userId = _currentUser.UserId;
        var collection = _mongoContext.GetCollection<UserSettings>(CollectionNames.UserSettings);

        var settings = await collection.Find(s => s.UserId == userId).FirstOrDefaultAsync(ct);

        if (settings is null)
        {
            settings = new UserSettings
            {
                UserId = userId,
                ProfileVisibility = request.ProfileVisibility,
                DmPermission = request.DmPermission,
                ShowOnlineStatus = request.ShowOnlineStatus,
                DefaultLookingForTeam = request.DefaultLookingForTeam,
            };
            await collection.InsertOneAsync(settings, cancellationToken: ct);
        }
        else
        {
            var update = Builders<UserSettings>.Update
                .Set(s => s.ProfileVisibility, request.ProfileVisibility)
                .Set(s => s.DmPermission, request.DmPermission)
                .Set(s => s.ShowOnlineStatus, request.ShowOnlineStatus)
                .Set(s => s.DefaultLookingForTeam, request.DefaultLookingForTeam)
                .Set(s => s.UpdatedAt, DateTime.UtcNow);

            await collection.UpdateOneAsync(s => s.Id == settings.Id, update, cancellationToken: ct);
            settings.ProfileVisibility = request.ProfileVisibility;
            settings.DmPermission = request.DmPermission;
            settings.ShowOnlineStatus = request.ShowOnlineStatus;
            settings.DefaultLookingForTeam = request.DefaultLookingForTeam;
        }

        return Result<UserSettingsResponse>.Success(GetMySettingsQueryHandler.MapToResponse(settings));
    }
}
