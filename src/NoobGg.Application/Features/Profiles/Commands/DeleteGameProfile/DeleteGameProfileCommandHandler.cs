using MediatR;
using MongoDB.Driver;
using NoobGg.Application.Common.Constants;
using NoobGg.Application.Common.Interfaces;
using NoobGg.Application.Common.Models;
using NoobGg.Domain.Entities;

namespace NoobGg.Application.Features.Profiles.Commands.DeleteGameProfile;

public class DeleteGameProfileCommandHandler : IRequestHandler<DeleteGameProfileCommand, Result>
{
    private readonly IMongoContext _mongoContext;
    private readonly ICurrentUser _currentUser;

    public DeleteGameProfileCommandHandler(IMongoContext mongoContext, ICurrentUser currentUser)
    {
        _mongoContext = mongoContext;
        _currentUser = currentUser;
    }

    public async Task<Result> Handle(DeleteGameProfileCommand request, CancellationToken ct)
    {
        if (!_currentUser.IsAuthenticated || _currentUser.UserId is null)
            return Result.Fail("Unauthorized", 401);

        var userId = _currentUser.UserId;
        var gameProfiles = _mongoContext.GetCollection<UserGameProfile>(CollectionNames.UserGameProfiles);

        var deleted = await gameProfiles.DeleteOneAsync(
            gp => gp.Id == request.Id && gp.UserId == userId,
            ct);

        return deleted.DeletedCount > 0
            ? Result.Success()
            : Result.Fail("Game profile not found", 404);
    }
}
