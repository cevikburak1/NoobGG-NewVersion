using MediatR;
using MongoDB.Driver;
using NoobGg.Application.Common.Constants;
using NoobGg.Application.Common.Interfaces;
using NoobGg.Application.Common.Models;
using NoobGg.Application.Features.DirectMessages.DTOs;
using NoobGg.Domain.Entities;

namespace NoobGg.Application.Features.DirectMessages.Queries.GetConversations;

public class GetConversationsQueryHandler
    : IRequestHandler<GetConversationsQuery, Result<List<ConversationResponse>>>
{
    private readonly IMongoContext _mongoContext;
    private readonly ICurrentUser _currentUser;

    public GetConversationsQueryHandler(IMongoContext mongoContext, ICurrentUser currentUser)
    {
        _mongoContext = mongoContext;
        _currentUser = currentUser;
    }

    public async Task<Result<List<ConversationResponse>>> Handle(
        GetConversationsQuery request, CancellationToken ct)
    {
        if (!_currentUser.IsAuthenticated || _currentUser.UserId is null)
            return Result<List<ConversationResponse>>.Unauthorized();

        var userId = _currentUser.UserId;
        var conversations = _mongoContext.GetCollection<Conversation>(CollectionNames.Conversations);
        var users = _mongoContext.GetCollection<User>(CollectionNames.Users);
        var profiles = _mongoContext.GetCollection<UserProfile>(CollectionNames.UserProfiles);

        var filter = Builders<Conversation>.Filter.Or(
            Builders<Conversation>.Filter.Eq(c => c.Participant1Id, userId),
            Builders<Conversation>.Filter.Eq(c => c.Participant2Id, userId));

        var convList = await conversations
            .Find(filter)
            .SortByDescending(c => c.LastMessageAt)
            .ToListAsync(ct);

        var partnerIds = convList
            .Select(c => c.Participant1Id == userId ? c.Participant2Id : c.Participant1Id)
            .Distinct()
            .ToList();

        var partnerUsers = partnerIds.Count > 0
            ? await users.Find(Builders<User>.Filter.In(u => u.Id, partnerIds)).ToListAsync(ct)
            : new List<User>();

        var partnerProfiles = partnerIds.Count > 0
            ? await profiles.Find(Builders<UserProfile>.Filter.In(p => p.UserId, partnerIds)).ToListAsync(ct)
            : new List<UserProfile>();

        var userMap = partnerUsers.ToDictionary(u => u.Id);
        var profileMap = partnerProfiles.ToDictionary(p => p.UserId);

        var responses = convList.Select(c =>
        {
            var partnerId = c.Participant1Id == userId ? c.Participant2Id : c.Participant1Id;
            var unread = c.Participant1Id == userId ? c.Participant1UnreadCount : c.Participant2UnreadCount;
            userMap.TryGetValue(partnerId, out var partner);
            profileMap.TryGetValue(partnerId, out var partnerProfile);

            return new ConversationResponse
            {
                Id = c.Id,
                PartnerId = partnerId,
                PartnerUsername = partner?.Username ?? "Unknown",
                PartnerAvatarUrl = partnerProfile?.AvatarUrl,
                LastMessageContent = c.LastMessageContent,
                LastMessageSenderId = c.LastMessageSenderId,
                LastMessageAt = c.LastMessageAt,
                UnreadCount = unread
            };
        }).ToList();

        return Result<List<ConversationResponse>>.Success(responses);
    }
}
