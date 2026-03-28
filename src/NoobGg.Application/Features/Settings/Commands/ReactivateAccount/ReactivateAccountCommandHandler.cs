using MediatR;
using MongoDB.Driver;
using NoobGg.Application.Common.Constants;
using NoobGg.Application.Common.Interfaces;
using NoobGg.Application.Common.Models;
using NoobGg.Domain.Entities;

namespace NoobGg.Application.Features.Settings.Commands.ReactivateAccount;

public class ReactivateAccountCommandHandler : IRequestHandler<ReactivateAccountCommand, Result>
{
    private readonly IMongoContext _mongoContext;
    private readonly ICurrentUser _currentUser;

    public ReactivateAccountCommandHandler(IMongoContext mongoContext, ICurrentUser currentUser)
    {
        _mongoContext = mongoContext;
        _currentUser = currentUser;
    }

    public async Task<Result> Handle(ReactivateAccountCommand request, CancellationToken ct)
    {
        if (!_currentUser.IsAuthenticated || _currentUser.UserId is null)
            return Result.Fail("Unauthorized", 401);

        var userId = _currentUser.UserId;
        var collection = _mongoContext.GetCollection<UserSettings>(CollectionNames.UserSettings);

        var settings = await collection.Find(s => s.UserId == userId).FirstOrDefaultAsync(ct);

        if (settings is null || !settings.IsDeactivated)
            return Result.Fail("Account is not deactivated.");

        var update = Builders<UserSettings>.Update
            .Set(s => s.IsDeactivated, false)
            .Set(s => s.DeactivatedAt, (DateTime?)null)
            .Set(s => s.DeactivationReason, (string?)null)
            .Set(s => s.UpdatedAt, DateTime.UtcNow);

        await collection.UpdateOneAsync(s => s.Id == settings.Id, update, cancellationToken: ct);

        return Result.Success();
    }
}
