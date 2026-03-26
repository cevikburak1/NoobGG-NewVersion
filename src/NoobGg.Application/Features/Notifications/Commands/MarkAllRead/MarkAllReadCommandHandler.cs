using MediatR;
using MongoDB.Driver;
using NoobGg.Application.Common.Constants;
using NoobGg.Application.Common.Interfaces;
using NoobGg.Application.Common.Models;
using NoobGg.Domain.Entities;

namespace NoobGg.Application.Features.Notifications.Commands.MarkAllRead;

public class MarkAllReadCommandHandler : IRequestHandler<MarkAllReadCommand, Result>
{
    private readonly IMongoContext _mongoContext;
    private readonly ICurrentUser _currentUser;

    public MarkAllReadCommandHandler(IMongoContext mongoContext, ICurrentUser currentUser)
    {
        _mongoContext = mongoContext;
        _currentUser = currentUser;
    }

    public async Task<Result> Handle(MarkAllReadCommand request, CancellationToken ct)
    {
        if (!_currentUser.IsAuthenticated || _currentUser.UserId is null)
            return Result.Fail("Unauthorized", 401);

        var collection = _mongoContext.GetCollection<Notification>(CollectionNames.Notifications);

        var filter = Builders<Notification>.Filter.And(
            Builders<Notification>.Filter.Eq(n => n.UserId, _currentUser.UserId),
            Builders<Notification>.Filter.Eq(n => n.IsRead, false));

        var update = Builders<Notification>.Update
            .Set(n => n.IsRead, true)
            .Set(n => n.ReadAt, DateTime.UtcNow)
            .Set(n => n.UpdatedAt, DateTime.UtcNow);

        await collection.UpdateManyAsync(filter, update, cancellationToken: ct);

        return Result.Success();
    }
}
