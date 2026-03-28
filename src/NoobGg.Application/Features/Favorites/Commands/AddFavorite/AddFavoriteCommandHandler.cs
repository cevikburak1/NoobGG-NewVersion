using MediatR;
using MongoDB.Driver;
using NoobGg.Application.Common.Constants;
using NoobGg.Application.Common.Interfaces;
using NoobGg.Application.Common.Models;
using NoobGg.Domain.Entities;

namespace NoobGg.Application.Features.Favorites.Commands.AddFavorite;

public class AddFavoriteCommandHandler : IRequestHandler<AddFavoriteCommand, Result>
{
    private readonly IMongoContext _mongoContext;
    private readonly ICurrentUser _currentUser;

    public AddFavoriteCommandHandler(IMongoContext mongoContext, ICurrentUser currentUser)
    {
        _mongoContext = mongoContext;
        _currentUser = currentUser;
    }

    public async Task<Result> Handle(AddFavoriteCommand request, CancellationToken ct)
    {
        if (!_currentUser.IsAuthenticated || _currentUser.UserId is null)
            return Result.Fail("Unauthorized", 401);

        var userId = _currentUser.UserId;

        if (userId == request.FavoriteUserId)
            return Result.Fail("You cannot favorite yourself");

        var users = _mongoContext.GetCollection<User>(CollectionNames.Users);
        var targetExists = await users.Find(u => u.Id == request.FavoriteUserId).AnyAsync(ct);
        if (!targetExists)
            return Result.Fail("User not found", 404);

        var blocks = _mongoContext.GetCollection<Block>(CollectionNames.Blocks);
        var hasBlock = await blocks.Find(b =>
            (b.BlockerId == userId && b.BlockedUserId == request.FavoriteUserId) ||
            (b.BlockerId == request.FavoriteUserId && b.BlockedUserId == userId)
        ).AnyAsync(ct);

        if (hasBlock)
            return Result.Fail("Cannot favorite this user");

        var favorites = _mongoContext.GetCollection<Favorite>(CollectionNames.Favorites);
        var favorite = new Favorite
        {
            UserId = userId,
            FavoriteUserId = request.FavoriteUserId
        };

        try
        {
            await favorites.InsertOneAsync(favorite, cancellationToken: ct);
        }
        catch (MongoWriteException ex) when (ex.WriteError.Category == ServerErrorCategory.DuplicateKey)
        {
            // Idempotent — already favorited
        }

        return Result.Success();
    }
}
