using MediatR;
using MongoDB.Driver;
using NoobGg.Application.Common.Constants;
using NoobGg.Application.Common.Interfaces;
using NoobGg.Application.Common.Models;
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

        var memberResponses = members.Select(m => new RoomMemberResponse(
            m.UserId,
            usernameMap.GetValueOrDefault(m.UserId, "Unknown"),
            m.Role.ToString(),
            m.JoinedAt)).ToList();

        var response = new RoomDetailResponse(
            room.Id,
            room.Title,
            room.Description,
            room.GameId,
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
            memberResponses);

        return Result<RoomDetailResponse>.Success(response);
    }
}
