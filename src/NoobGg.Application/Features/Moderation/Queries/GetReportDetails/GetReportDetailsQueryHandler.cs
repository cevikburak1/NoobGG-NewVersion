using MediatR;
using MongoDB.Driver;
using NoobGg.Application.Common.Constants;
using NoobGg.Application.Common.Interfaces;
using NoobGg.Application.Common.Models;
using NoobGg.Application.Features.Reports.DTOs;
using NoobGg.Domain.Entities;

namespace NoobGg.Application.Features.Moderation.Queries.GetReportDetails;

public class GetReportDetailsQueryHandler
    : IRequestHandler<GetReportDetailsQuery, Result<ReportDetailResponse>>
{
    private readonly IMongoContext _mongoContext;
    private readonly ICurrentUser _currentUser;

    public GetReportDetailsQueryHandler(IMongoContext mongoContext, ICurrentUser currentUser)
    {
        _mongoContext = mongoContext;
        _currentUser = currentUser;
    }

    public async Task<Result<ReportDetailResponse>> Handle(
        GetReportDetailsQuery request, CancellationToken ct)
    {
        if (!_currentUser.IsAuthenticated)
            return Result<ReportDetailResponse>.Unauthorized();

        if (_currentUser.Role is not ("Moderator" or "Admin"))
            return Result<ReportDetailResponse>.Forbidden();

        var reports = _mongoContext.GetCollection<Report>(CollectionNames.Reports);
        var report = await reports.Find(r => r.Id == request.ReportId).FirstOrDefaultAsync(ct);

        if (report is null)
            return Result<ReportDetailResponse>.NotFound("Report not found");

        var users = _mongoContext.GetCollection<User>(CollectionNames.Users);

        var relevantIds = new List<string> { report.ReporterId, report.ReportedUserId };
        if (report.ReviewedBy is not null) relevantIds.Add(report.ReviewedBy);

        var userDocs = await users
            .Find(Builders<User>.Filter.In(u => u.Id, relevantIds.Distinct()))
            .ToListAsync(ct);
        var userMap = userDocs.ToDictionary(u => u.Id, u => u.Username);

        string? roomTitle = null;
        if (report.RoomId is not null)
        {
            var rooms = _mongoContext.GetCollection<Room>(CollectionNames.Rooms);
            var room = await rooms.Find(r => r.Id == report.RoomId).FirstOrDefaultAsync(ct);
            roomTitle = room?.Title;
        }

        var response = new ReportDetailResponse(
            report.Id,
            report.ReporterId,
            userMap.GetValueOrDefault(report.ReporterId),
            report.TargetType.ToString(),
            report.ReportedUserId,
            userMap.GetValueOrDefault(report.ReportedUserId),
            report.RoomId,
            roomTitle,
            report.Reason.ToString(),
            report.Description,
            report.Status.ToString(),
            report.ReviewedBy,
            report.ReviewedBy is not null ? userMap.GetValueOrDefault(report.ReviewedBy) : null,
            report.ReviewNote,
            report.ReviewedAt,
            report.CreatedAt);

        return Result<ReportDetailResponse>.Success(response);
    }
}
