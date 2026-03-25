using MediatR;
using MongoDB.Driver;
using NoobGg.Application.Common.Constants;
using NoobGg.Application.Common.Interfaces;
using NoobGg.Application.Common.Models;

namespace NoobGg.Application.Features.Auth.Commands.Logout;

public class LogoutCommandHandler : IRequestHandler<LogoutCommand, Result>
{
    private readonly IMongoContext _mongoContext;
    private readonly ICurrentUser _currentUser;

    public LogoutCommandHandler(IMongoContext mongoContext, ICurrentUser currentUser)
    {
        _mongoContext = mongoContext;
        _currentUser = currentUser;
    }

    public async Task<Result> Handle(LogoutCommand request, CancellationToken ct)
    {
        if (!_currentUser.IsAuthenticated || _currentUser.UserId is null)
            return Result.Fail("Not authenticated", 401);

        var refreshTokens = _mongoContext.GetCollection<Domain.Entities.RefreshToken>(CollectionNames.RefreshTokens);

        var filter = Builders<Domain.Entities.RefreshToken>.Filter.And(
            Builders<Domain.Entities.RefreshToken>.Filter.Eq(rt => rt.UserId, _currentUser.UserId),
            Builders<Domain.Entities.RefreshToken>.Filter.Eq(rt => rt.RevokedAt, null));

        var update = Builders<Domain.Entities.RefreshToken>.Update
            .Set(rt => rt.RevokedAt, DateTime.UtcNow)
            .Set(rt => rt.UpdatedAt, DateTime.UtcNow);

        await refreshTokens.UpdateManyAsync(filter, update, cancellationToken: ct);

        return Result.Success();
    }
}
