using MediatR;
using MongoDB.Driver;
using NoobGg.Application.Common.Constants;
using NoobGg.Application.Common.Interfaces;
using NoobGg.Application.Common.Models;
using NoobGg.Application.Features.Guilds.DTOs;
using NoobGg.Domain.Entities;
using NoobGg.Domain.Enums;

namespace NoobGg.Application.Features.Guilds.Queries.GetPendingJoinRequests;

public class GetPendingJoinRequestsQueryHandler
    : IRequestHandler<GetPendingJoinRequestsQuery, Result<List<GuildJoinRequestResponse>>>
{
    private readonly IMongoContext _mongoContext;
    private readonly ICurrentUser _currentUser;

    public GetPendingJoinRequestsQueryHandler(IMongoContext mongoContext, ICurrentUser currentUser)
    {
        _mongoContext = mongoContext;
        _currentUser = currentUser;
    }

    public async Task<Result<List<GuildJoinRequestResponse>>> Handle(
        GetPendingJoinRequestsQuery request, CancellationToken ct)
    {
        if (!_currentUser.IsAuthenticated || _currentUser.UserId is null)
            return Result<List<GuildJoinRequestResponse>>.Fail("Unauthorized", 401);

        var userId = _currentUser.UserId;
        var guildMembers = _mongoContext.GetCollection<GuildMember>(CollectionNames.GuildMembers);

        var currentMember = await guildMembers
            .Find(m => m.GuildId == request.GuildId && m.UserId == userId)
            .FirstOrDefaultAsync(ct);

        if (currentMember is null || (currentMember.Role != GuildMemberRole.Owner && currentMember.Role != GuildMemberRole.Admin))
            return Result<List<GuildJoinRequestResponse>>.Fail("Only guild owners and admins can view join requests");

        var joinRequests = _mongoContext.GetCollection<GuildJoinRequest>(CollectionNames.GuildJoinRequests);
        var pendingRequests = await joinRequests
            .Find(r => r.GuildId == request.GuildId && r.Status == GuildJoinRequestStatus.Pending)
            .SortByDescending(r => r.CreatedAt)
            .ToListAsync(ct);

        if (pendingRequests.Count == 0)
            return Result<List<GuildJoinRequestResponse>>.Success([]);

        var requestUserIds = pendingRequests.Select(r => r.UserId).ToList();
        var users = _mongoContext.GetCollection<User>(CollectionNames.Users);
        var userDocs = await users.Find(u => requestUserIds.Contains(u.Id)).ToListAsync(ct);
        var usernameMap = userDocs.ToDictionary(u => u.Id, u => u.Username);

        var profiles = _mongoContext.GetCollection<UserProfile>(CollectionNames.UserProfiles);
        var profileDocs = await profiles.Find(p => requestUserIds.Contains(p.UserId)).ToListAsync(ct);
        var avatarMap = profileDocs.ToDictionary(p => p.UserId, p => p.AvatarUrl);

        var responses = pendingRequests.Select(r => new GuildJoinRequestResponse(
            r.Id,
            r.GuildId,
            r.UserId,
            usernameMap.GetValueOrDefault(r.UserId, "Unknown"),
            avatarMap.GetValueOrDefault(r.UserId),
            r.Message,
            r.Status.ToString(),
            r.CreatedAt)).ToList();

        return Result<List<GuildJoinRequestResponse>>.Success(responses);
    }
}
