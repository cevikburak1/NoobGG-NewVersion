using MediatR;
using MongoDB.Driver;
using NoobGg.Application.Common.Constants;
using NoobGg.Application.Common.Interfaces;
using NoobGg.Application.Common.Models;
using NoobGg.Application.Features.Reports.DTOs;
using NoobGg.Domain.Entities;

namespace NoobGg.Application.Features.Moderation.Queries.GetReports;

public class GetReportsQueryHandler
    : IRequestHandler<GetReportsQuery, Result<PagedResult<ReportResponse>>>
{
    private readonly IMongoContext _mongoContext;
    private readonly ICurrentUser _currentUser;

    public GetReportsQueryHandler(IMongoContext mongoContext, ICurrentUser currentUser)
    {
        _mongoContext = mongoContext;
        _currentUser = currentUser;
    }

    public async Task<Result<PagedResult<ReportResponse>>> Handle(
        GetReportsQuery request, CancellationToken ct)
    {
        if (!_currentUser.IsAuthenticated)
            return Result<PagedResult<ReportResponse>>.Unauthorized();

        if (_currentUser.Role is not ("Moderator" or "Admin"))
            return Result<PagedResult<ReportResponse>>.Forbidden();

        var reports = _mongoContext.GetCollection<Report>(CollectionNames.Reports);

        var filterBuilder = Builders<Report>.Filter;
        var filters = new List<FilterDefinition<Report>>();

        if (request.Status.HasValue)
            filters.Add(filterBuilder.Eq(r => r.Status, request.Status.Value));

        if (request.TargetType.HasValue)
            filters.Add(filterBuilder.Eq(r => r.TargetType, request.TargetType.Value));

        if (request.Reason.HasValue)
            filters.Add(filterBuilder.Eq(r => r.Reason, request.Reason.Value));

        var combinedFilter = filters.Count > 0
            ? filterBuilder.And(filters)
            : filterBuilder.Empty;

        var totalCount = await reports.CountDocumentsAsync(combinedFilter, cancellationToken: ct);
        var skip = (request.Page - 1) * request.PageSize;

        var docs = await reports
            .Find(combinedFilter)
            .SortByDescending(r => r.CreatedAt)
            .Skip(skip)
            .Limit(request.PageSize)
            .ToListAsync(ct);

        // Resolve usernames and room titles for display
        var userIds = docs.Select(r => r.ReportedUserId).Where(id => !string.IsNullOrEmpty(id)).Distinct().ToList();
        var roomIds = docs.Select(r => r.RoomId).Where(id => id is not null).Distinct().ToList();

        var users = _mongoContext.GetCollection<User>(CollectionNames.Users);
        var userDocs = await users
            .Find(Builders<User>.Filter.In(u => u.Id, userIds))
            .ToListAsync(ct);
        var userMap = userDocs.ToDictionary(u => u.Id, u => u.Username);

        var roomMap = new Dictionary<string, string>();
        if (roomIds.Count > 0)
        {
            var rooms = _mongoContext.GetCollection<Room>(CollectionNames.Rooms);
            var roomDocs = await rooms
                .Find(Builders<Room>.Filter.In(r => r.Id, roomIds!))
                .ToListAsync(ct);
            roomMap = roomDocs.ToDictionary(r => r.Id, r => r.Title);
        }

        var items = docs.Select(r => new ReportResponse(
            r.Id,
            r.TargetType.ToString(),
            r.ReportedUserId,
            userMap.GetValueOrDefault(r.ReportedUserId),
            r.RoomId,
            r.RoomId is not null ? roomMap.GetValueOrDefault(r.RoomId) : null,
            r.Reason.ToString(),
            r.Description,
            r.Status.ToString(),
            r.CreatedAt
        )).ToList();

        return Result<PagedResult<ReportResponse>>.Success(new PagedResult<ReportResponse>
        {
            Items = items,
            TotalCount = (int)totalCount,
            Page = request.Page,
            PageSize = request.PageSize
        });
    }
}
