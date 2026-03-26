using MediatR;
using MongoDB.Driver;
using NoobGg.Application.Common.Constants;
using NoobGg.Application.Common.Interfaces;
using NoobGg.Application.Common.Models;
using NoobGg.Domain.Entities;

namespace NoobGg.Application.Features.DirectMessages.Commands.MarkConversationRead;

public class MarkConversationReadCommandHandler
    : IRequestHandler<MarkConversationReadCommand, Result>
{
    private readonly IMongoContext _mongoContext;
    private readonly ICurrentUser _currentUser;

    public MarkConversationReadCommandHandler(IMongoContext mongoContext, ICurrentUser currentUser)
    {
        _mongoContext = mongoContext;
        _currentUser = currentUser;
    }

    public async Task<Result> Handle(MarkConversationReadCommand request, CancellationToken ct)
    {
        if (!_currentUser.IsAuthenticated || _currentUser.UserId is null)
            return Result.Fail("Unauthorized", 401);

        var userId = _currentUser.UserId;
        var conversations = _mongoContext.GetCollection<Conversation>(CollectionNames.Conversations);
        var messages = _mongoContext.GetCollection<DirectMessage>(CollectionNames.DirectMessages);

        var conv = await conversations.Find(c => c.Id == request.ConversationId).FirstOrDefaultAsync(ct);
        if (conv is null)
            return Result.Fail("Conversation not found", 404);

        if (conv.Participant1Id != userId && conv.Participant2Id != userId)
            return Result.Fail("Forbidden", 403);

        var isP1 = conv.Participant1Id == userId;

        await conversations.UpdateOneAsync(
            c => c.Id == conv.Id,
            Builders<Conversation>.Update.Set(
                isP1 ? c => c.Participant1UnreadCount : c => c.Participant2UnreadCount, 0),
            cancellationToken: ct);

        var msgFilter = Builders<DirectMessage>.Filter.And(
            Builders<DirectMessage>.Filter.Eq(m => m.ConversationId, request.ConversationId),
            Builders<DirectMessage>.Filter.Ne(m => m.SenderId, userId),
            Builders<DirectMessage>.Filter.Eq(m => m.IsRead, false));

        await messages.UpdateManyAsync(
            msgFilter,
            Builders<DirectMessage>.Update
                .Set(m => m.IsRead, true)
                .Set(m => m.ReadAt, DateTime.UtcNow),
            cancellationToken: ct);

        return Result.Success();
    }
}
