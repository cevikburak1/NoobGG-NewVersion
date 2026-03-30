using MediatR;
using MongoDB.Driver;
using NoobGg.Application.Common.Constants;
using NoobGg.Application.Common.Interfaces;
using NoobGg.Application.Common.Models;
using NoobGg.Application.Features.Guilds.DTOs;
using NoobGg.Domain.Entities;
using NoobGg.Domain.Enums;

namespace NoobGg.Application.Features.Guilds.Queries.GetGuildDetail;

public class GetGuildDetailQueryHandler : IRequestHandler<GetGuildDetailQuery, Result<GuildDetailResponse>>
{
    private readonly IMongoContext _mongoContext;
    private readonly ICurrentUser _currentUser;

    public GetGuildDetailQueryHandler(IMongoContext mongoContext, ICurrentUser currentUser)
    {
        _mongoContext = mongoContext;
        _currentUser = currentUser;
    }

    public async Task<Result<GuildDetailResponse>> Handle(GetGuildDetailQuery request, CancellationToken ct)
    {
        var guilds = _mongoContext.GetCollection<Guild>(CollectionNames.Guilds);
        var guildMembers = _mongoContext.GetCollection<GuildMember>(CollectionNames.GuildMembers);
        var users = _mongoContext.GetCollection<User>(CollectionNames.Users);

        var guild = await guilds.Find(g => g.Id == request.GuildId).FirstOrDefaultAsync(ct);
        if (guild is null)
            return Result<GuildDetailResponse>.NotFound("Guild not found");

        var members = await guildMembers.Find(m => m.GuildId == request.GuildId)
            .SortBy(m => m.JoinedAt)
            .ToListAsync(ct);

        var memberUserIds = members.Select(m => m.UserId).ToList();
        var userDocs = await users.Find(u => memberUserIds.Contains(u.Id)).ToListAsync(ct);
        var usernameMap = userDocs.ToDictionary(u => u.Id, u => u.Username);

        var profiles = _mongoContext.GetCollection<UserProfile>(CollectionNames.UserProfiles);
        var memberProfiles = await profiles.Find(p => memberUserIds.Contains(p.UserId)).ToListAsync(ct);
        var avatarMap = memberProfiles.ToDictionary(p => p.UserId, p => p.AvatarUrl);

        var memberResponses = members.Select(m => new GuildMemberResponse(
            m.UserId,
            usernameMap.GetValueOrDefault(m.UserId, "Unknown"),
            avatarMap.GetValueOrDefault(m.UserId),
            m.Role.ToString(),
            m.JoinedAt)).ToList();

        var games = _mongoContext.GetCollection<Game>(CollectionNames.Games);
        var gameDocs = await games.Find(Builders<Game>.Filter.In(g => g.Id, guild.GameIds)).ToListAsync(ct);
        var gameInfos = gameDocs.Select(g => new GuildGameInfo(g.Id, g.Name, g.BackgroundImageUrl)).ToList();

        string? myJoinRequestStatus = null;
        var pendingJoinRequestCount = 0;

        var joinRequests = _mongoContext.GetCollection<GuildJoinRequest>(CollectionNames.GuildJoinRequests);

        if (_currentUser.IsAuthenticated && _currentUser.UserId is not null)
        {
            var currentUserId = _currentUser.UserId;

            var myRequest = await joinRequests
                .Find(r => r.GuildId == request.GuildId
                            && r.UserId == currentUserId
                            && r.Status == GuildJoinRequestStatus.Pending)
                .FirstOrDefaultAsync(ct);

            myJoinRequestStatus = myRequest?.Status.ToString();

            var currentMember = members.FirstOrDefault(m => m.UserId == currentUserId);
            if (currentMember is not null &&
                (currentMember.Role == GuildMemberRole.Owner || currentMember.Role == GuildMemberRole.Admin))
            {
                pendingJoinRequestCount = (int)await joinRequests
                    .CountDocumentsAsync(
                        r => r.GuildId == request.GuildId && r.Status == GuildJoinRequestStatus.Pending, cancellationToken: ct);
            }
        }

        var response = new GuildDetailResponse(
            guild.Id,
            guild.Name,
            guild.Tag,
            guild.Description,
            guild.CreatorId,
            guild.IsPublic,
            guild.MaxMembers,
            guild.CurrentMemberCount,
            guild.Region.ToString(),
            guild.Language.ToString(),
            guild.GameIds,
            gameInfos,
            guild.CreatedAt,
            memberResponses,
            myJoinRequestStatus,
            pendingJoinRequestCount);

        return Result<GuildDetailResponse>.Success(response);
    }
}
