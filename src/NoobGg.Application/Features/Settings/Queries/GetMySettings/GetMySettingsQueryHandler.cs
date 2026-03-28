using MediatR;
using MongoDB.Driver;
using NoobGg.Application.Common.Constants;
using NoobGg.Application.Common.Interfaces;
using NoobGg.Application.Common.Models;
using NoobGg.Application.Features.Settings.DTOs;
using NoobGg.Domain.Entities;

namespace NoobGg.Application.Features.Settings.Queries.GetMySettings;

public class GetMySettingsQueryHandler : IRequestHandler<GetMySettingsQuery, Result<UserSettingsResponse>>
{
    private readonly IMongoContext _mongoContext;
    private readonly ICurrentUser _currentUser;

    public GetMySettingsQueryHandler(IMongoContext mongoContext, ICurrentUser currentUser)
    {
        _mongoContext = mongoContext;
        _currentUser = currentUser;
    }

    public async Task<Result<UserSettingsResponse>> Handle(GetMySettingsQuery request, CancellationToken ct)
    {
        if (!_currentUser.IsAuthenticated || _currentUser.UserId is null)
            return Result<UserSettingsResponse>.Unauthorized();

        var userId = _currentUser.UserId;
        var collection = _mongoContext.GetCollection<UserSettings>(CollectionNames.UserSettings);

        var settings = await collection.Find(s => s.UserId == userId).FirstOrDefaultAsync(ct);

        if (settings is null)
        {
            settings = new UserSettings { UserId = userId };
            await collection.InsertOneAsync(settings, cancellationToken: ct);
        }

        return Result<UserSettingsResponse>.Success(MapToResponse(settings));
    }

    internal static UserSettingsResponse MapToResponse(UserSettings s) => new()
    {
        ProfileVisibility = s.ProfileVisibility,
        DmPermission = s.DmPermission,
        ShowOnlineStatus = s.ShowOnlineStatus,
        DefaultLookingForTeam = s.DefaultLookingForTeam,
        NotifyFriendRequests = s.NotifyFriendRequests,
        NotifyDirectMessages = s.NotifyDirectMessages,
        NotifyRoomActivity = s.NotifyRoomActivity,
        NotifySystemMessages = s.NotifySystemMessages,
        IsDeactivated = s.IsDeactivated,
        DeactivatedAt = s.DeactivatedAt,
        DeletionRequestedAt = s.DeletionRequestedAt,
    };
}
