using MediatR;
using MongoDB.Driver;
using NoobGg.Application.Common.Constants;
using NoobGg.Application.Common.Interfaces;
using NoobGg.Application.Common.Models;
using NoobGg.Application.Features.DirectMessages.DTOs;
using NoobGg.Domain.Entities;

namespace NoobGg.Application.Features.DirectMessages.Commands.SendDirectMessage;

public class SendDirectMessageCommandHandler
    : IRequestHandler<SendDirectMessageCommand, Result<DirectMessageResponse>>
{
    private readonly IMongoContext _mongoContext;
    private readonly ICurrentUser _currentUser;

    public SendDirectMessageCommandHandler(IMongoContext mongoContext, ICurrentUser currentUser)
    {
        _mongoContext = mongoContext;
        _currentUser = currentUser;
    }

    public async Task<Result<DirectMessageResponse>> Handle(
        SendDirectMessageCommand request, CancellationToken ct)
    {
        if (!_currentUser.IsAuthenticated || _currentUser.UserId is null)
            return Result<DirectMessageResponse>.Unauthorized();

        var userId = _currentUser.UserId;
        var conversations = _mongoContext.GetCollection<Conversation>(CollectionNames.Conversations);
        var messages = _mongoContext.GetCollection<DirectMessage>(CollectionNames.DirectMessages);
        var blocks = _mongoContext.GetCollection<Block>(CollectionNames.Blocks);
        var users = _mongoContext.GetCollection<User>(CollectionNames.Users);

        var conv = await conversations.Find(c => c.Id == request.ConversationId).FirstOrDefaultAsync(ct);
        if (conv is null)
            return Result<DirectMessageResponse>.NotFound("Conversation not found");

        if (conv.Participant1Id != userId && conv.Participant2Id != userId)
            return Result<DirectMessageResponse>.Forbidden();

        var partnerId = conv.Participant1Id == userId ? conv.Participant2Id : conv.Participant1Id;

        var isBlocked = await blocks.Find(b =>
            (b.BlockerId == userId && b.BlockedUserId == partnerId) ||
            (b.BlockerId == partnerId && b.BlockedUserId == userId)
        ).AnyAsync(ct);

        if (isBlocked)
            return Result<DirectMessageResponse>.Fail("Cannot send message. User is blocked.");

        var sender = await users.Find(u => u.Id == userId).FirstOrDefaultAsync(ct);

        var dm = new DirectMessage
        {
            ConversationId = conv.Id,
            SenderId = userId,
            SenderUsername = sender?.Username ?? _currentUser.Username ?? "Unknown",
            Content = request.Content.Trim()
        };

        await messages.InsertOneAsync(dm, cancellationToken: ct);

        var isP1 = conv.Participant1Id == userId;
        var convUpdate = Builders<Conversation>.Update
            .Set(c => c.LastMessageContent, dm.Content.Length > 100 ? dm.Content[..100] : dm.Content)
            .Set(c => c.LastMessageSenderId, userId)
            .Set(c => c.LastMessageAt, dm.CreatedAt)
            .Set(c => c.UpdatedAt, DateTime.UtcNow)
            .Inc(isP1 ? c => c.Participant2UnreadCount : c => c.Participant1UnreadCount, 1);

        await conversations.UpdateOneAsync(c => c.Id == conv.Id, convUpdate, cancellationToken: ct);

        return Result<DirectMessageResponse>.Created(new DirectMessageResponse
        {
            Id = dm.Id,
            ConversationId = dm.ConversationId,
            SenderId = dm.SenderId,
            SenderUsername = dm.SenderUsername,
            Content = dm.Content,
            IsRead = false,
            CreatedAt = dm.CreatedAt
        });
    }
}
