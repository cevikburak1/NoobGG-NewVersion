using MediatR;
using MongoDB.Driver;
using NoobGg.Application.Common.Constants;
using NoobGg.Application.Common.Interfaces;
using NoobGg.Application.Common.Models;
using NoobGg.Application.Features.Blocks.DTOs;
using NoobGg.Domain.Entities;

namespace NoobGg.Application.Features.Blocks.Queries.GetBlockedUsers;

public class GetBlockedUsersQueryHandler
    : IRequestHandler<GetBlockedUsersQuery, Result<List<BlockedUserResponse>>>
{
    private readonly IMongoContext _mongoContext;
    private readonly ICurrentUser _currentUser;

    public GetBlockedUsersQueryHandler(IMongoContext mongoContext, ICurrentUser currentUser)
    {
        _mongoContext = mongoContext;
        _currentUser = currentUser;
    }

    public async Task<Result<List<BlockedUserResponse>>> Handle(
        GetBlockedUsersQuery request, CancellationToken ct)
    {
        if (!_currentUser.IsAuthenticated || _currentUser.UserId is null)
            return Result<List<BlockedUserResponse>>.Unauthorized();

        var userId = _currentUser.UserId;
        var blocks = _mongoContext.GetCollection<Block>(CollectionNames.Blocks);
        var users = _mongoContext.GetCollection<User>(CollectionNames.Users);

        var myBlocks = await blocks
            .Find(b => b.BlockerId == userId)
            .SortByDescending(b => b.CreatedAt)
            .ToListAsync(ct);

        if (myBlocks.Count == 0)
            return Result<List<BlockedUserResponse>>.Success([]);

        var blockedIds = myBlocks.Select(b => b.BlockedUserId).ToList();
        var blockedUsers = await users
            .Find(Builders<User>.Filter.In(u => u.Id, blockedIds))
            .ToListAsync(ct);

        var userMap = blockedUsers.ToDictionary(u => u.Id);

        var result = myBlocks.Select(b => new BlockedUserResponse(
            b.Id,
            b.BlockedUserId,
            userMap.TryGetValue(b.BlockedUserId, out var u) ? u.Username : "Deleted User",
            b.CreatedAt
        )).ToList();

        return Result<List<BlockedUserResponse>>.Success(result);
    }
}
