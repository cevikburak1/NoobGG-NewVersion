using MediatR;
using MongoDB.Driver;
using NoobGg.Application.Common.Constants;
using NoobGg.Application.Common.Interfaces;
using NoobGg.Application.Common.Models;
using NoobGg.Application.Features.DirectMessages.DTOs;
using NoobGg.Domain.Entities;

namespace NoobGg.Application.Features.DirectMessages.Queries.GetMessages;

public class GetMessagesQueryHandler
    : IRequestHandler<GetMessagesQuery, Result<List<DirectMessageResponse>>>
{
    private readonly IMongoContext _mongoContext;
    private readonly ICurrentUser _currentUser;

    public GetMessagesQueryHandler(IMongoContext mongoContext, ICurrentUser currentUser)
    {
        _mongoContext = mongoContext;
        _currentUser = currentUser;
    }

    public async Task<Result<List<DirectMessageResponse>>> Handle(
        GetMessagesQuery request, CancellationToken ct)
    {
        if (!_currentUser.IsAuthenticated || _currentUser.UserId is null)
            return Result<List<DirectMessageResponse>>.Unauthorized();

        var userId = _currentUser.UserId;
        var conversations = _mongoContext.GetCollection<Conversation>(CollectionNames.Conversations);
        var messages = _mongoContext.GetCollection<DirectMessage>(CollectionNames.DirectMessages);

        var conv = await conversations.Find(c => c.Id == request.ConversationId).FirstOrDefaultAsync(ct);
        if (conv is null)
            return Result<List<DirectMessageResponse>>.NotFound("Conversation not found");

        if (conv.Participant1Id != userId && conv.Participant2Id != userId)
            return Result<List<DirectMessageResponse>>.Forbidden();

        var msgList = await messages
            .Find(m => m.ConversationId == request.ConversationId)
            .SortByDescending(m => m.CreatedAt)
            .Skip((request.Page - 1) * request.PageSize)
            .Limit(request.PageSize)
            .ToListAsync(ct);

        msgList.Reverse();

        var responses = msgList.Select(m => new DirectMessageResponse
        {
            Id = m.Id,
            ConversationId = m.ConversationId,
            SenderId = m.SenderId,
            SenderUsername = m.SenderUsername,
            Content = m.Content,
            IsRead = m.IsRead,
            CreatedAt = m.CreatedAt
        }).ToList();

        return Result<List<DirectMessageResponse>>.Success(responses);
    }
}
