using MediatR;
using MongoDB.Driver;
using NoobGg.Application.Common.Constants;
using NoobGg.Application.Common.Interfaces;
using NoobGg.Application.Common.Models;
using NoobGg.Domain.Entities;

namespace NoobGg.Application.Features.Notifications.Commands.MarkNotificationRead;

public class MarkNotificationReadCommandHandler
    : IRequestHandler<MarkNotificationReadCommand, Result>
{
    private readonly IMongoContext _mongoContext;
    private readonly ICurrentUser _currentUser;

    public MarkNotificationReadCommandHandler(IMongoContext mongoContext, ICurrentUser currentUser)
    {
        _mongoContext = mongoContext;
        _currentUser = currentUser;
    }

    public async Task<Result> Handle(MarkNotificationReadCommand request, CancellationToken ct)
    {
        if (!_currentUser.IsAuthenticated || _currentUser.UserId is null)
            return Result.Fail("Unauthorized", 401);

        var collection = _mongoContext.GetCollection<Notification>(CollectionNames.Notifications);

        var filter = Builders<Notification>.Filter.And(
            Builders<Notification>.Filter.Eq(n => n.Id, request.NotificationId),
            Builders<Notification>.Filter.Eq(n => n.UserId, _currentUser.UserId));

        var update = Builders<Notification>.Update
            .Set(n => n.IsRead, true)
            .Set(n => n.ReadAt, DateTime.UtcNow)
            .Set(n => n.UpdatedAt, DateTime.UtcNow);

        var result = await collection.UpdateOneAsync(filter, update, cancellationToken: ct);

        return result.ModifiedCount > 0
            ? Result.Success()
            : Result.Fail("Notification not found", 404);
    }
}
