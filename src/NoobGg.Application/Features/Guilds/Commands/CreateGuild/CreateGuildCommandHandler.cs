using MediatR;
using MongoDB.Driver;
using NoobGg.Application.Common.Constants;
using NoobGg.Application.Common.Interfaces;
using NoobGg.Application.Common.Models;
using NoobGg.Application.Features.Guilds.DTOs;
using NoobGg.Domain.Entities;
using NoobGg.Domain.Enums;

namespace NoobGg.Application.Features.Guilds.Commands.CreateGuild;

public class CreateGuildCommandHandler : IRequestHandler<CreateGuildCommand, Result<GuildDetailResponse>>
{
    private readonly IMongoContext _mongoContext;
    private readonly ICurrentUser _currentUser;

    public CreateGuildCommandHandler(IMongoContext mongoContext, ICurrentUser currentUser)
    {
        _mongoContext = mongoContext;
        _currentUser = currentUser;
    }

    public async Task<Result<GuildDetailResponse>> Handle(CreateGuildCommand request, CancellationToken ct)
    {
        if (!_currentUser.IsAuthenticated || _currentUser.UserId is null)
            return Result<GuildDetailResponse>.Unauthorized();

        var userId = _currentUser.UserId;
        var guilds = _mongoContext.GetCollection<Guild>(CollectionNames.Guilds);
        var guildMembers = _mongoContext.GetCollection<GuildMember>(CollectionNames.GuildMembers);

        var tagUpper = request.Tag.Trim().ToUpperInvariant();
        var tagExists = await guilds.Find(g => g.Tag == tagUpper).AnyAsync(ct);
        if (tagExists)
            return Result<GuildDetailResponse>.Fail("This guild tag is already taken");

        var ownsGuild = await guilds.Find(g => g.CreatorId == userId).AnyAsync(ct);
        if (ownsGuild)
            return Result<GuildDetailResponse>.Fail("You already own a guild. Transfer or disband it first.");

        var games = _mongoContext.GetCollection<Game>(CollectionNames.Games);
        var validGameIds = new List<string>();
        var gameInfos = new List<GuildGameInfo>();

        if (request.GameIds.Count > 0)
        {
            var gameDocs = await games.Find(g => request.GameIds.Contains(g.Id) && g.IsActive).ToListAsync(ct);
            validGameIds = gameDocs.Select(g => g.Id).ToList();
            gameInfos = gameDocs.Select(g => new GuildGameInfo(g.Id, g.Name, g.BackgroundImageUrl)).ToList();
        }

        var guild = new Guild
        {
            Name = request.Name.Trim(),
            Tag = tagUpper,
            Description = request.Description?.Trim(),
            CreatorId = userId,
            IsPublic = request.IsPublic,
            Region = request.Region,
            Language = request.Language,
            GameIds = validGameIds,
            MaxMembers = 50,
            CurrentMemberCount = 1
        };

        await guilds.InsertOneAsync(guild, cancellationToken: ct);

        var ownerMember = new GuildMember
        {
            GuildId = guild.Id,
            UserId = userId,
            Role = GuildMemberRole.Owner,
            JoinedAt = DateTime.UtcNow
        };

        await guildMembers.InsertOneAsync(ownerMember, cancellationToken: ct);

        var users = _mongoContext.GetCollection<User>(CollectionNames.Users);
        var user = await users.Find(u => u.Id == userId).FirstOrDefaultAsync(ct);

        var profilesCol = _mongoContext.GetCollection<UserProfile>(CollectionNames.UserProfiles);
        var ownerProfile = await profilesCol.Find(p => p.UserId == userId).FirstOrDefaultAsync(ct);

        var memberResponse = new GuildMemberResponse(
            userId,
            user?.Username ?? "Unknown",
            ownerProfile?.AvatarUrl,
            ownerMember.Role.ToString(),
            ownerMember.JoinedAt);

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
            [memberResponse],
            null,
            0);

        return Result<GuildDetailResponse>.Created(response);
    }
}
