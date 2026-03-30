using MediatR;
using MongoDB.Driver;
using NoobGg.Application.Common.Constants;
using NoobGg.Application.Common.Interfaces;
using NoobGg.Application.Common.Models;
using NoobGg.Application.Features.Elo.Helpers;
using NoobGg.Application.Features.Rooms.DTOs;
using NoobGg.Domain.Entities;

namespace NoobGg.Application.Features.Rooms.Queries.GetRoomDetails;

public class GetRoomDetailsQueryHandler : IRequestHandler<GetRoomDetailsQuery, Result<RoomDetailResponse>>
{
    private readonly IMongoContext _mongoContext;

    public GetRoomDetailsQueryHandler(IMongoContext mongoContext)
    {
        _mongoContext = mongoContext;
    }

    public async Task<Result<RoomDetailResponse>> Handle(GetRoomDetailsQuery request, CancellationToken ct)
    {
        var rooms = _mongoContext.GetCollection<Room>(CollectionNames.Rooms);
        var roomMembers = _mongoContext.GetCollection<RoomMember>(CollectionNames.RoomMembers);
        var users = _mongoContext.GetCollection<User>(CollectionNames.Users);

        var room = await rooms.Find(r => r.Id == request.RoomId).FirstOrDefaultAsync(ct);
        if (room is null)
            return Result<RoomDetailResponse>.NotFound("Room not found");

        var members = await roomMembers.Find(m => m.RoomId == request.RoomId)
            .SortBy(m => m.JoinedAt)
            .ToListAsync(ct);

        var memberUserIds = members.Select(m => m.UserId).ToList();
        var userDocs = await users.Find(u => memberUserIds.Contains(u.Id)).ToListAsync(ct);
        var usernameMap = userDocs.ToDictionary(u => u.Id, u => u.Username);

        var profiles = _mongoContext.GetCollection<UserProfile>(CollectionNames.UserProfiles);
        var memberProfiles = await profiles.Find(p => memberUserIds.Contains(p.UserId)).ToListAsync(ct);
        var avatarMap = memberProfiles.ToDictionary(p => p.UserId, p => p.AvatarUrl);

        var gameProfiles = _mongoContext.GetCollection<UserGameProfile>(CollectionNames.UserGameProfiles);
        var memberGameProfiles = await gameProfiles
            .Find(gp => memberUserIds.Contains(gp.UserId) && gp.GameId == room.GameId)
            .ToListAsync(ct);
        var eloMap = memberGameProfiles.ToDictionary(gp => gp.UserId);

        var memberResponses = members.Select(m =>
        {
            eloMap.TryGetValue(m.UserId, out var gp);
            return new RoomMemberResponse(
                m.UserId,
                usernameMap.GetValueOrDefault(m.UserId, "Unknown"),
                avatarMap.GetValueOrDefault(m.UserId),
                m.Role.ToString(),
                m.JoinedAt,
                gp?.EloPoints,
                gp?.RankTier.ToString());
        }).ToList();

        var games = _mongoContext.GetCollection<Game>(CollectionNames.Games);
        var game = await games.Find(g => g.Id == room.GameId).FirstOrDefaultAsync(ct);

        int? averageElo = null;
        string? averageRankTier = null;
        var eloValues = memberResponses.Where(m => m.EloPoints.HasValue).Select(m => m.EloPoints!.Value).ToList();
        if (eloValues.Count > 0)
        {
            averageElo = (int)Math.Round(eloValues.Average());
            averageRankTier = EloCalculator.GetTier(averageElo.Value).ToString();
        }

        if (averageElo != room.AverageElo || averageRankTier != room.AverageRankTier)
        {
            _ = rooms.UpdateOneAsync(
                Builders<Room>.Filter.Eq(r => r.Id, room.Id),
                Builders<Room>.Update
                    .Set(r => r.AverageElo, averageElo)
                    .Set(r => r.AverageRankTier, averageRankTier),
                cancellationToken: CancellationToken.None);
        }

        var response = new RoomDetailResponse(
            room.Id,
            room.Title,
            room.Description,
            room.GameId,
            game?.Name,
            game?.BackgroundImageUrl,
            room.CreatorId,
            room.IsPublic,
            room.MaxMembers,
            room.CurrentMemberCount,
            room.Region.ToString(),
            room.Language.ToString(),
            room.RankRange,
            room.Tags,
            room.Status.ToString(),
            room.VoiceChannelId,
            room.CreatedAt,
            memberResponses,
            averageElo,
            averageRankTier);

        return Result<RoomDetailResponse>.Success(response);
    }
}
