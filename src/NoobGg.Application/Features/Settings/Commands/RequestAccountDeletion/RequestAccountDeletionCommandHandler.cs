using MediatR;
using MongoDB.Driver;
using NoobGg.Application.Common.Constants;
using NoobGg.Application.Common.Interfaces;
using NoobGg.Application.Common.Models;
using NoobGg.Domain.Entities;

namespace NoobGg.Application.Features.Settings.Commands.RequestAccountDeletion;

public class RequestAccountDeletionCommandHandler : IRequestHandler<RequestAccountDeletionCommand, Result>
{
    private readonly IMongoContext _mongoContext;
    private readonly ICurrentUser _currentUser;

    public RequestAccountDeletionCommandHandler(IMongoContext mongoContext, ICurrentUser currentUser)
    {
        _mongoContext = mongoContext;
        _currentUser = currentUser;
    }

    public async Task<Result> Handle(RequestAccountDeletionCommand request, CancellationToken ct)
    {
        if (!_currentUser.IsAuthenticated || _currentUser.UserId is null)
            return Result.Fail("Unauthorized", 401);

        var userId = _currentUser.UserId;
        var collection = _mongoContext.GetCollection<UserSettings>(CollectionNames.UserSettings);

        var settings = await collection.Find(s => s.UserId == userId).FirstOrDefaultAsync(ct);

        if (settings is null)
        {
            settings = new UserSettings
            {
                UserId = userId,
                DeletionRequestedAt = DateTime.UtcNow,
            };
            await collection.InsertOneAsync(settings, cancellationToken: ct);
        }
        else
        {
            if (settings.DeletionRequestedAt is not null)
                return Result.Fail("Deletion has already been requested.");

            var update = Builders<UserSettings>.Update
                .Set(s => s.DeletionRequestedAt, DateTime.UtcNow)
                .Set(s => s.UpdatedAt, DateTime.UtcNow);

            await collection.UpdateOneAsync(s => s.Id == settings.Id, update, cancellationToken: ct);
        }

        return Result.Success();
    }
}
