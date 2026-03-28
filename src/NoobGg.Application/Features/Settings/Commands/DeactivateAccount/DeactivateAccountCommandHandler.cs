using MediatR;
using MongoDB.Driver;
using NoobGg.Application.Common.Constants;
using NoobGg.Application.Common.Interfaces;
using NoobGg.Application.Common.Models;
using NoobGg.Domain.Entities;

namespace NoobGg.Application.Features.Settings.Commands.DeactivateAccount;

public class DeactivateAccountCommandHandler : IRequestHandler<DeactivateAccountCommand, Result>
{
    private readonly IMongoContext _mongoContext;
    private readonly ICurrentUser _currentUser;

    public DeactivateAccountCommandHandler(IMongoContext mongoContext, ICurrentUser currentUser)
    {
        _mongoContext = mongoContext;
        _currentUser = currentUser;
    }

    public async Task<Result> Handle(DeactivateAccountCommand request, CancellationToken ct)
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
                IsDeactivated = true,
                DeactivatedAt = DateTime.UtcNow,
                DeactivationReason = request.Reason,
            };
            await collection.InsertOneAsync(settings, cancellationToken: ct);
        }
        else
        {
            if (settings.IsDeactivated)
                return Result.Fail("Account is already deactivated.");

            var update = Builders<UserSettings>.Update
                .Set(s => s.IsDeactivated, true)
                .Set(s => s.DeactivatedAt, DateTime.UtcNow)
                .Set(s => s.DeactivationReason, request.Reason)
                .Set(s => s.UpdatedAt, DateTime.UtcNow);

            await collection.UpdateOneAsync(s => s.Id == settings.Id, update, cancellationToken: ct);
        }

        return Result.Success();
    }
}
