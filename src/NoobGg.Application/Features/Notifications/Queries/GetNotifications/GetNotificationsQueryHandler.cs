using MediatR;
using MongoDB.Driver;
using NoobGg.Application.Common.Constants;
using NoobGg.Application.Common.Interfaces;
using NoobGg.Application.Common.Models;
using NoobGg.Application.Features.Notifications.DTOs;
using NoobGg.Domain.Entities;

namespace NoobGg.Application.Features.Notifications.Queries.GetNotifications;

public class GetNotificationsQueryHandler
    : IRequestHandler<GetNotificationsQuery, Result<PagedResult<NotificationResponse>>>
{
    private readonly IMongoContext _mongoContext;
    private readonly ICurrentUser _currentUser;

    public GetNotificationsQueryHandler(IMongoContext mongoContext, ICurrentUser currentUser)
    {
        _mongoContext = mongoContext;
        _currentUser = currentUser;
    }

    public async Task<Result<PagedResult<NotificationResponse>>> Handle(
        GetNotificationsQuery request, CancellationToken ct)
    {
        if (!_currentUser.IsAuthenticated || _currentUser.UserId is null)
            return Result<PagedResult<NotificationResponse>>.Unauthorized();

        var userId = _currentUser.UserId;
        var collection = _mongoContext.GetCollection<Notification>(CollectionNames.Notifications);

        var filters = new List<FilterDefinition<Notification>>
        {
            Builders<Notification>.Filter.Eq(n => n.UserId, userId)
        };

        if (request.UnreadOnly == true)
            filters.Add(Builders<Notification>.Filter.Eq(n => n.IsRead, false));

        var filter = Builders<Notification>.Filter.And(filters);
        var totalCount = await collection.CountDocumentsAsync(filter, cancellationToken: ct);

        var items = await collection.Find(filter)
            .SortByDescending(n => n.CreatedAt)
            .Skip((request.Page - 1) * request.PageSize)
            .Limit(request.PageSize)
            .ToListAsync(ct);

        var result = new PagedResult<NotificationResponse>
        {
            Items = items.Select(MapToResponse).ToList(),
            TotalCount = (int)totalCount,
            Page = request.Page,
            PageSize = request.PageSize
        };

        return Result<PagedResult<NotificationResponse>>.Success(result);
    }

    private static NotificationResponse MapToResponse(Notification n) => new()
    {
        Id = n.Id,
        Type = n.Type,
        Title = n.Title,
        Body = n.Body,
        Data = n.Data,
        IsRead = n.IsRead,
        ReadAt = n.ReadAt,
        CreatedAt = n.CreatedAt
    };
}
