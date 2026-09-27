using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;
using NoobGg.Application.Common.Constants;
using NoobGg.Application.Common.Interfaces;
using NoobGg.Domain.Entities;
using NoobGg.Domain.Enums;

namespace NoobGg.Api.Controllers;

[Authorize]
[Route("api/sessions")]
public class SessionsController(
    IMongoContext mongo,
    ICurrentUser currentUser,
    INotificationService notifications,
    IRoomNotificationService roomNotifications) : ApiControllerBase
{
    [HttpGet("mine")]
    public async Task<IActionResult> Mine(CancellationToken ct)
    {
        var userId = currentUser.UserId!;
        var sessions = mongo.GetCollection<GameSession>(CollectionNames.GameSessions);
        var items = await sessions.Find(s => s.ParticipantIds.Contains(userId))
            .SortByDescending(s => s.EndedAt).Limit(30).ToListAsync(ct);
        return Ok(items);
    }

    [HttpPost("{id}/feedback")]
    public async Task<IActionResult> Feedback(string id, [FromBody] SessionFeedbackRequest request, CancellationToken ct)
    {
        var userId = currentUser.UserId!;
        var sessions = mongo.GetCollection<GameSession>(CollectionNames.GameSessions);
        var session = await sessions.Find(s => s.Id == id && s.ParticipantIds.Contains(userId))
            .FirstOrDefaultAsync(ct);
        if (session is null) return NotFound();
        var tags = request.Tags ?? [];
        if (tags.Count > 3 || tags.Any(t => string.IsNullOrWhiteSpace(t) || t.Length > 32))
            return BadRequest("Choose at most three short tags");

        var feedback = mongo.GetCollection<SessionFeedback>(CollectionNames.SessionFeedback);
        var update = Builders<SessionFeedback>.Update
            .Set(f => f.WouldPlayAgain, request.WouldPlayAgain)
            .Set(f => f.Tags, tags.Distinct().ToList())
            .Set(f => f.UpdatedAt, DateTime.UtcNow)
            .SetOnInsert(f => f.SessionId, id)
            .SetOnInsert(f => f.UserId, userId);
        await feedback.UpdateOneAsync(f => f.SessionId == id && f.UserId == userId,
            update, new UpdateOptions { IsUpsert = true }, ct);
        return NoContent();
    }

    [HttpPost("{id}/replay")]
    public async Task<IActionResult> Replay(string id, CancellationToken ct)
    {
        var userId = currentUser.UserId!;
        var sessions = mongo.GetCollection<GameSession>(CollectionNames.GameSessions);
        var session = await sessions.Find(s => s.Id == id && s.OwnerId == userId).FirstOrDefaultAsync(ct);
        if (session is null) return NotFound();
        var rooms = mongo.GetCollection<Room>(CollectionNames.Rooms);
        if (session.ReplayRoomId is { } previousRoomId)
        {
            var existingRoom = await rooms.Find(r => r.Id == previousRoomId).AnyAsync(ct);
            if (existingRoom) return Ok(new { roomId = previousRoomId });
            await sessions.UpdateOneAsync(s => s.Id == id && s.ReplayRoomId == previousRoomId,
                Builders<GameSession>.Update.Set(s => s.ReplayRoomId, (string?)null), cancellationToken: ct);
        }

        var room = new Room
        {
            Title = $"Again: {session.RoomTitle}",
            Description = "Reunion with your previous squad",
            GameId = session.GameId,
            CreatorId = userId,
            IsPublic = false,
            Region = session.Region,
            Language = session.Language,
            MaxMembers = Math.Max(5, session.ParticipantIds.Count),
            CurrentMemberCount = 1
        };
        var claim = await sessions.UpdateOneAsync(
            s => s.Id == id && s.OwnerId == userId && s.ReplayRoomId == null,
            Builders<GameSession>.Update.Set(s => s.ReplayRoomId, room.Id), cancellationToken: ct);
        if (claim.ModifiedCount == 0)
        {
            session = await sessions.Find(s => s.Id == id).FirstAsync(ct);
            return Ok(new { roomId = session.ReplayRoomId });
        }

        var members = mongo.GetCollection<RoomMember>(CollectionNames.RoomMembers);
        try
        {
            await rooms.InsertOneAsync(room, cancellationToken: ct);
            await members.InsertOneAsync(new RoomMember { RoomId = room.Id, UserId = userId, Role = RoomMemberRole.Owner }, cancellationToken: ct);
            var invites = mongo.GetCollection<RoomInvite>(CollectionNames.RoomInvites);
            foreach (var participantId in session.ParticipantIds.Where(p => p != userId))
            {
                var invite = new RoomInvite { RoomId = room.Id, InviterId = userId, InvitedUserId = participantId };
                await invites.InsertOneAsync(invite, cancellationToken: ct);
                await notifications.CreateAsync(participantId, NotificationType.RoomInvite,
                    "Play together again", $"Your previous squad invited you to {room.Title}",
                    new Dictionary<string, string> { ["roomId"] = room.Id, ["inviteId"] = invite.Id }, ct);
            }
            await roomNotifications.NotifyRoomListChangedAsync(ct);
            return Ok(new { roomId = room.Id });
        }
        catch
        {
            await mongo.GetCollection<RoomInvite>(CollectionNames.RoomInvites).DeleteManyAsync(i => i.RoomId == room.Id, ct);
            await members.DeleteManyAsync(m => m.RoomId == room.Id, ct);
            await rooms.DeleteOneAsync(r => r.Id == room.Id, ct);
            await sessions.UpdateOneAsync(s => s.Id == id && s.ReplayRoomId == room.Id,
                Builders<GameSession>.Update.Set(s => s.ReplayRoomId, (string?)null), cancellationToken: ct);
            throw;
        }
    }
}

public record SessionFeedbackRequest(bool WouldPlayAgain, List<string>? Tags);
