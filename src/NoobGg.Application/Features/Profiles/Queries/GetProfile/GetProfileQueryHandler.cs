using MediatR;
using MongoDB.Driver;
using NoobGg.Application.Common.Constants;
using NoobGg.Application.Common.Interfaces;
using NoobGg.Application.Common.Models;
using NoobGg.Application.Features.Profiles.DTOs;
using NoobGg.Domain.Entities;
using NoobGg.Domain.Enums;

namespace NoobGg.Application.Features.Profiles.Queries.GetProfile;

public class GetProfileQueryHandler : IRequestHandler<GetProfileQuery, Result<ProfileDetailResponse>>
{
    private readonly IMongoContext _mongoContext;
    private readonly ICurrentUser _currentUser;
    private readonly IPresenceTracker _presenceTracker;

    public GetProfileQueryHandler(IMongoContext mongoContext, ICurrentUser currentUser, IPresenceTracker presenceTracker)
    {
        _mongoContext = mongoContext;
        _currentUser = currentUser;
        _presenceTracker = presenceTracker;
    }

    public async Task<Result<ProfileDetailResponse>> Handle(GetProfileQuery request, CancellationToken ct)
    {
        var users = _mongoContext.GetCollection<User>(CollectionNames.Users);
        var profiles = _mongoContext.GetCollection<UserProfile>(CollectionNames.UserProfiles);
        var gameProfiles = _mongoContext.GetCollection<UserGameProfile>(CollectionNames.UserGameProfiles);
        var games = _mongoContext.GetCollection<Game>(CollectionNames.Games);
        var blocks = _mongoContext.GetCollection<Block>(CollectionNames.Blocks);
        var rooms = _mongoContext.GetCollection<Room>(CollectionNames.Rooms);
        var roomMembers = _mongoContext.GetCollection<RoomMember>(CollectionNames.RoomMembers);
        var settingsCol = _mongoContext.GetCollection<UserSettings>(CollectionNames.UserSettings);
        var friendships = _mongoContext.GetCollection<Friendship>(CollectionNames.Friendships);

        var user = await users.Find(u => u.Id == request.UserId).FirstOrDefaultAsync(ct);
        if (user is null)
            return Result<ProfileDetailResponse>.NotFound("User not found");

        var isOwnProfile = _currentUser.IsAuthenticated && _currentUser.UserId == request.UserId;

        var targetSettings = await settingsCol.Find(s => s.UserId == request.UserId).FirstOrDefaultAsync(ct);

        if (targetSettings is { IsDeactivated: true } && !isOwnProfile)
        {
            return Result<ProfileDetailResponse>.Success(BuildRestrictedProfile(
                user, "This account is deactivated."));
        }

        var isBlocked = false;
        var isBlockedByThem = false;
        if (_currentUser.IsAuthenticated && !isOwnProfile && _currentUser.UserId is not null)
        {
            isBlocked = await blocks.Find(b =>
                b.BlockerId == _currentUser.UserId && b.BlockedUserId == request.UserId
            ).AnyAsync(ct);

            isBlockedByThem = await blocks.Find(b =>
                b.BlockerId == request.UserId && b.BlockedUserId == _currentUser.UserId
            ).AnyAsync(ct);
        }

        if (isBlockedByThem)
        {
            return Result<ProfileDetailResponse>.Success(new ProfileDetailResponse
            {
                UserId = user.Id,
                Username = user.Username,
                IsBlockedByThem = true,
                IsRestricted = true,
                RestrictedReason = "This user has blocked you."
            });
        }

        if (!isOwnProfile && targetSettings is not null)
        {
            var visibility = targetSettings.ProfileVisibility;
            if (visibility == ProfileVisibility.Private)
            {
                return Result<ProfileDetailResponse>.Success(BuildRestrictedProfile(
                    user, "This profile is private.", isBlocked: isBlocked));
            }

            if (visibility == ProfileVisibility.FriendsOnly)
            {
                var areFriends = false;
                if (_currentUser.IsAuthenticated && _currentUser.UserId is not null)
                {
                    areFriends = await friendships.Find(f =>
                        f.Status == FriendshipStatus.Accepted &&
                        ((f.RequesterId == _currentUser.UserId && f.AddresseeId == request.UserId) ||
                         (f.RequesterId == request.UserId && f.AddresseeId == _currentUser.UserId))
                    ).AnyAsync(ct);
                }

                if (!areFriends)
                {
                    return Result<ProfileDetailResponse>.Success(BuildRestrictedProfile(
                        user, "This profile is only visible to friends.", isBlocked: isBlocked));
                }
            }
        }

        var profile = await profiles.Find(p => p.UserId == request.UserId).FirstOrDefaultAsync(ct);

        var gpList = await gameProfiles.Find(gp => gp.UserId == request.UserId).ToListAsync(ct);
        var gameIds = gpList.Select(gp => gp.GameId).Distinct().ToList();
        var gameList = gameIds.Count > 0
            ? await games.Find(Builders<Game>.Filter.In(g => g.Id, gameIds)).ToListAsync(ct)
            : new List<Game>();
        var gameMap = gameList.ToDictionary(g => g.Id);

        var gameResponses = gpList.Select(gp =>
        {
            gameMap.TryGetValue(gp.GameId, out var game);
            return new GameProfileResponse
            {
                Id = gp.Id,
                UserId = gp.UserId,
                GameId = gp.GameId,
                GameName = game?.Name ?? "Unknown",
                GameImageUrl = game?.BackgroundImageUrl,
                Rank = gp.Rank,
                Role = gp.Role,
                Region = gp.Region.ToString(),
                ExperienceLevel = gp.ExperienceLevel.ToString(),
                CommunicationPreference = gp.CommunicationPreference.ToString(),
                HoursPlayed = gp.HoursPlayed,
                LookingForTeam = gp.LookingForTeam,
                Note = gp.Note
            };
        }).ToList();

        var roomsCreated = await rooms.CountDocumentsAsync(
            Builders<Room>.Filter.Eq(r => r.CreatorId, request.UserId), cancellationToken: ct);

        var roomsJoined = await roomMembers.CountDocumentsAsync(
            Builders<RoomMember>.Filter.Eq(m => m.UserId, request.UserId), cancellationToken: ct);

        var topGp = gpList.FirstOrDefault();

        var showOnline = targetSettings?.ShowOnlineStatus ?? true;
        var isOnline = isOwnProfile || showOnline
            ? _presenceTracker.IsOnline(request.UserId)
            : false;

        string? friendshipStatus = null;
        string? friendshipId = null;
        var isFriendRequestSentByMe = false;
        var isFavorited = false;

        if (_currentUser.IsAuthenticated && !isOwnProfile && _currentUser.UserId is not null)
        {
            var friendship = await friendships.Find(f =>
                (f.RequesterId == _currentUser.UserId && f.AddresseeId == request.UserId) ||
                (f.RequesterId == request.UserId && f.AddresseeId == _currentUser.UserId)
            ).FirstOrDefaultAsync(ct);

            if (friendship is not null)
            {
                friendshipStatus = friendship.Status switch
                {
                    FriendshipStatus.Pending => "Pending",
                    FriendshipStatus.Accepted => "Accepted",
                    _ => null
                };
                friendshipId = friendship.Id;
                isFriendRequestSentByMe = friendship.RequesterId == _currentUser.UserId;
            }

            var favorites = _mongoContext.GetCollection<Favorite>(CollectionNames.Favorites);
            isFavorited = await favorites.Find(f =>
                f.UserId == _currentUser.UserId && f.FavoriteUserId == request.UserId
            ).AnyAsync(ct);
        }

        return Result<ProfileDetailResponse>.Success(new ProfileDetailResponse
        {
            UserId = user.Id,
            Username = user.Username,
            DisplayName = profile?.DisplayName,
            AvatarUrl = profile?.AvatarUrl,
            BannerUrl = profile?.BannerUrl,
            Bio = profile?.Bio,
            Country = profile?.Country,
            Timezone = profile?.Timezone,
            Region = topGp?.Region.ToString(),
            Language = topGp?.Languages.FirstOrDefault().ToString(),
            ExperienceLevel = topGp?.ExperienceLevel.ToString(),
            CommunicationPreference = topGp?.CommunicationPreference.ToString(),
            PlaySchedule = profile?.Availability != null
                ? FormatAvailability(profile.Availability)
                : null,
            IsProfileComplete = user.IsProfileComplete,
            CreatedAt = user.CreatedAt,
            Games = gameResponses,
            Stats = new ProfileStats
            {
                RoomsCreated = (int)roomsCreated,
                RoomsJoined = (int)roomsJoined,
                GamesPlayed = gpList.Count
            },
            IsOwnProfile = isOwnProfile,
            IsOnline = isOnline,
            IsBlocked = isBlocked,
            IsBlockedByThem = isBlockedByThem,
            FriendshipStatus = friendshipStatus,
            FriendshipId = friendshipId,
            IsFriendRequestSentByMe = isFriendRequestSentByMe,
            IsFavorited = isFavorited
        });
    }

    private static ProfileDetailResponse BuildRestrictedProfile(User user, string reason, bool isBlocked = false) => new()
    {
        UserId = user.Id,
        Username = user.Username,
        IsRestricted = true,
        RestrictedReason = reason,
        IsBlocked = isBlocked,
    };

    private static string FormatAvailability(Domain.ValueObjects.Availability avail)
    {
        var parts = new List<string>();
        if (avail.Weekdays is not null)
            parts.Add($"Weekdays {avail.Weekdays.From}-{avail.Weekdays.To}");
        if (avail.Weekends is not null)
            parts.Add($"Weekends {avail.Weekends.From}-{avail.Weekends.To}");
        return string.Join(", ", parts);
    }
}
