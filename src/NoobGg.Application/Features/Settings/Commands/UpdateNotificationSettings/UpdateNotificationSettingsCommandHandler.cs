using MediatR;
using MongoDB.Driver;
using NoobGg.Application.Common.Constants;
using NoobGg.Application.Common.Interfaces;
using NoobGg.Application.Common.Models;
using NoobGg.Application.Features.Settings.DTOs;
using NoobGg.Application.Features.Settings.Queries.GetMySettings;
using NoobGg.Domain.Entities;

namespace NoobGg.Application.Features.Settings.Commands.UpdateNotificationSettings;

public class UpdateNotificationSettingsCommandHandler
    : IRequestHandler<UpdateNotificationSettingsCommand, Result<UserSettingsResponse>>
{
    private readonly IMongoContext _mongoContext;
    private readonly ICurrentUser _currentUser;

    public UpdateNotificationSettingsCommandHandler(IMongoContext mongoContext, ICurrentUser currentUser)
    {
        _mongoContext = mongoContext;
        _currentUser = currentUser;
    }

    public async Task<Result<UserSettingsResponse>> Handle(UpdateNotificationSettingsCommand request, CancellationToken ct)
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
                NotifyFriendRequests = request.NotifyFriendRequests,
                NotifyDirectMessages = request.NotifyDirectMessages,
                NotifyRoomActivity = request.NotifyRoomActivity,
                NotifySystemMessages = request.NotifySystemMessages,
            };
            await collection.InsertOneAsync(settings, cancellationToken: ct);
        }
        else
        {
            var update = Builders<UserSettings>.Update
                .Set(s => s.NotifyFriendRequests, request.NotifyFriendRequests)
                .Set(s => s.NotifyDirectMessages, request.NotifyDirectMessages)
                .Set(s => s.NotifyRoomActivity, request.NotifyRoomActivity)
                .Set(s => s.NotifySystemMessages, request.NotifySystemMessages)
                .Set(s => s.UpdatedAt, DateTime.UtcNow);

            await collection.UpdateOneAsync(s => s.Id == settings.Id, update, cancellationToken: ct);
            settings.NotifyFriendRequests = request.NotifyFriendRequests;
            settings.NotifyDirectMessages = request.NotifyDirectMessages;
            settings.NotifyRoomActivity = request.NotifyRoomActivity;
            settings.NotifySystemMessages = request.NotifySystemMessages;
        }

        return Result<UserSettingsResponse>.Success(GetMySettingsQueryHandler.MapToResponse(settings));
    }
}
