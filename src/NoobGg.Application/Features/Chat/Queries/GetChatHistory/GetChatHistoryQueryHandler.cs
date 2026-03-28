using MediatR;
using MongoDB.Driver;
using NoobGg.Application.Common.Constants;
using NoobGg.Application.Common.Interfaces;
using NoobGg.Application.Common.Models;
using NoobGg.Application.Features.Chat.DTOs;
using NoobGg.Domain.Entities;

namespace NoobGg.Application.Features.Chat.Queries.GetChatHistory;

public class GetChatHistoryQueryHandler
    : IRequestHandler<GetChatHistoryQuery, Result<PagedResult<ChatMessageResponse>>>
{
    private readonly IMongoContext _mongoContext;
    private readonly ICurrentUser _currentUser;

    public GetChatHistoryQueryHandler(IMongoContext mongoContext, ICurrentUser currentUser)
    {
        _mongoContext = mongoContext;
        _currentUser = currentUser;
    }

    public async Task<Result<PagedResult<ChatMessageResponse>>> Handle(
        GetChatHistoryQuery request, CancellationToken ct)
    {
        if (!_currentUser.IsAuthenticated || _currentUser.UserId is null)
            return Result<PagedResult<ChatMessageResponse>>.Unauthorized();

        var roomMembers = _mongoContext.GetCollection<RoomMember>(CollectionNames.RoomMembers);
        var isMember = await roomMembers
            .Find(m => m.RoomId == request.RoomId && m.UserId == _currentUser.UserId)
            .AnyAsync(ct);

        if (!isMember)
            return Result<PagedResult<ChatMessageResponse>>.Forbidden("You are not a member of this room");

        var messages = _mongoContext.GetCollection<Message>(CollectionNames.Messages);

        var filterBuilder = Builders<Message>.Filter;
        var filter = filterBuilder.Eq(m => m.RoomId, request.RoomId)
                     & filterBuilder.Eq(m => m.IsDeleted, false);

        if (request.Before.HasValue)
            filter &= filterBuilder.Lt(m => m.CreatedAt, request.Before.Value);

        var totalCount = await messages.CountDocumentsAsync(filter, cancellationToken: ct);
        var skip = (request.Page - 1) * request.PageSize;

        var items = await messages
            .Find(filter)
            .SortByDescending(m => m.CreatedAt)
            .Skip(skip)
            .Limit(request.PageSize)
            .ToListAsync(ct);

        var senderIds = items.Select(m => m.SenderId).Distinct().ToList();
        var profiles = _mongoContext.GetCollection<UserProfile>(CollectionNames.UserProfiles);
        var senderProfiles = await profiles.Find(p => senderIds.Contains(p.UserId)).ToListAsync(ct);
        var avatarMap = senderProfiles.ToDictionary(p => p.UserId, p => p.AvatarUrl);

        var result = new PagedResult<ChatMessageResponse>
        {
            Items = items.Select(m => MapToResponse(m, avatarMap)).Reverse().ToList(),
            TotalCount = (int)totalCount,
            Page = request.Page,
            PageSize = request.PageSize
        };

        return Result<PagedResult<ChatMessageResponse>>.Success(result);
    }

    private static ChatMessageResponse MapToResponse(Message m, Dictionary<string, string?> avatarMap) => new()
    {
        Id = m.Id,
        RoomId = m.RoomId,
        SenderId = m.SenderId,
        SenderUsername = m.SenderUsername,
        SenderAvatarUrl = avatarMap.GetValueOrDefault(m.SenderId),
        Content = m.Content,
        Type = m.Type,
        IsEdited = m.IsEdited,
        CreatedAt = m.CreatedAt,
        EditedAt = m.EditedAt
    };
}
