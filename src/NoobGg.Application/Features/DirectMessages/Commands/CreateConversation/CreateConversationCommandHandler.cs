using MediatR;
using MongoDB.Driver;
using NoobGg.Application.Common.Constants;
using NoobGg.Application.Common.Interfaces;
using NoobGg.Application.Common.Models;
using NoobGg.Application.Features.DirectMessages.DTOs;
using NoobGg.Domain.Entities;

namespace NoobGg.Application.Features.DirectMessages.Commands.CreateConversation;

public class CreateConversationCommandHandler
    : IRequestHandler<CreateConversationCommand, Result<ConversationResponse>>
{
    private readonly IMongoContext _mongoContext;
    private readonly ICurrentUser _currentUser;

    public CreateConversationCommandHandler(IMongoContext mongoContext, ICurrentUser currentUser)
    {
        _mongoContext = mongoContext;
        _currentUser = currentUser;
    }

    public async Task<Result<ConversationResponse>> Handle(
        CreateConversationCommand request, CancellationToken ct)
    {
        if (!_currentUser.IsAuthenticated || _currentUser.UserId is null)
            return Result<ConversationResponse>.Unauthorized();

        var userId = _currentUser.UserId;
        if (userId == request.ParticipantId)
            return Result<ConversationResponse>.Fail("Cannot start a conversation with yourself");

        var conversations = _mongoContext.GetCollection<Conversation>(CollectionNames.Conversations);
        var users = _mongoContext.GetCollection<User>(CollectionNames.Users);
        var profiles = _mongoContext.GetCollection<UserProfile>(CollectionNames.UserProfiles);
        var blocks = _mongoContext.GetCollection<Block>(CollectionNames.Blocks);

        var partner = await users.Find(u => u.Id == request.ParticipantId).FirstOrDefaultAsync(ct);
        if (partner is null)
            return Result<ConversationResponse>.NotFound("User not found");

        var isBlocked = await blocks.Find(b =>
            (b.BlockerId == userId && b.BlockedUserId == request.ParticipantId) ||
            (b.BlockerId == request.ParticipantId && b.BlockedUserId == userId)
        ).AnyAsync(ct);

        if (isBlocked)
            return Result<ConversationResponse>.Fail("Cannot message this user");

        // Always store sorted so the unique index works regardless of who initiates
        var sortedIds = new[] { userId, request.ParticipantId }.OrderBy(id => id, StringComparer.Ordinal).ToArray();
        var p1 = sortedIds[0];
        var p2 = sortedIds[1];

        var existingFilter = Builders<Conversation>.Filter.And(
            Builders<Conversation>.Filter.Eq(c => c.Participant1Id, p1),
            Builders<Conversation>.Filter.Eq(c => c.Participant2Id, p2));

        var existing = await conversations.Find(existingFilter).FirstOrDefaultAsync(ct);
        if (existing is not null)
        {
            var partnerProfile = await profiles.Find(p => p.UserId == request.ParticipantId).FirstOrDefaultAsync(ct);
            var unread = existing.Participant1Id == userId
                ? existing.Participant1UnreadCount
                : existing.Participant2UnreadCount;

            return Result<ConversationResponse>.Success(new ConversationResponse
            {
                Id = existing.Id,
                PartnerId = request.ParticipantId,
                PartnerUsername = partner.Username,
                PartnerAvatarUrl = partnerProfile?.AvatarUrl,
                LastMessageContent = existing.LastMessageContent,
                LastMessageSenderId = existing.LastMessageSenderId,
                LastMessageAt = existing.LastMessageAt,
                UnreadCount = unread
            });
        }

        var conv = new Conversation
        {
            Participant1Id = p1,
            Participant2Id = p2
        };

        await conversations.InsertOneAsync(conv, cancellationToken: ct);

        var profile = await profiles.Find(p => p.UserId == request.ParticipantId).FirstOrDefaultAsync(ct);

        return Result<ConversationResponse>.Created(new ConversationResponse
        {
            Id = conv.Id,
            PartnerId = request.ParticipantId,
            PartnerUsername = partner.Username,
            PartnerAvatarUrl = profile?.AvatarUrl,
            LastMessageContent = null,
            LastMessageSenderId = null,
            LastMessageAt = null,
            UnreadCount = 0
        });
    }
}
