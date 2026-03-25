using MediatR;
using MongoDB.Driver;
using NoobGg.Application.Common.Constants;
using NoobGg.Application.Common.Interfaces;
using NoobGg.Application.Common.Models;
using NoobGg.Domain.Entities;
using NoobGg.Domain.Enums;

namespace NoobGg.Application.Features.Reports.Commands.CreateReport;

public class CreateReportCommandHandler : IRequestHandler<CreateReportCommand, Result>
{
    private readonly IMongoContext _mongoContext;
    private readonly ICurrentUser _currentUser;

    public CreateReportCommandHandler(IMongoContext mongoContext, ICurrentUser currentUser)
    {
        _mongoContext = mongoContext;
        _currentUser = currentUser;
    }

    public async Task<Result> Handle(CreateReportCommand request, CancellationToken ct)
    {
        if (!_currentUser.IsAuthenticated || _currentUser.UserId is null)
            return Result.Fail("Unauthorized", 401);

        var userId = _currentUser.UserId;
        var reportedUserId = request.ReportedUserId ?? string.Empty;

        if (request.TargetType == ReportTargetType.User)
        {
            if (userId == reportedUserId)
                return Result.Fail("You cannot report yourself");

            var users = _mongoContext.GetCollection<User>(CollectionNames.Users);
            var targetExists = await users.Find(u => u.Id == reportedUserId).AnyAsync(ct);
            if (!targetExists)
                return Result.Fail("Reported user not found", 404);
        }

        if (request.TargetType == ReportTargetType.Room)
        {
            var rooms = _mongoContext.GetCollection<Room>(CollectionNames.Rooms);
            var room = await rooms.Find(r => r.Id == request.RoomId).FirstOrDefaultAsync(ct);
            if (room is null)
                return Result.Fail("Room not found", 404);

            // For room reports, set reported user as room creator
            reportedUserId = room.CreatorId;
        }

        // Rate limit: max 5 pending reports from same user per day
        var reports = _mongoContext.GetCollection<Report>(CollectionNames.Reports);
        var oneDayAgo = DateTime.UtcNow.AddDays(-1);
        var filter = Builders<Report>.Filter.And(
            Builders<Report>.Filter.Eq(r => r.ReporterId, userId),
            Builders<Report>.Filter.Gt(r => r.CreatedAt, oneDayAgo),
            Builders<Report>.Filter.Eq(r => r.Status, ReportStatus.Pending));
        var recentCount = await reports.CountDocumentsAsync(filter, cancellationToken: ct);

        if (recentCount >= 5)
            return Result.Fail("You have too many pending reports. Please wait before submitting more.");

        var report = new Report
        {
            ReporterId = userId,
            TargetType = request.TargetType,
            ReportedUserId = reportedUserId,
            RoomId = request.RoomId,
            Reason = request.Reason,
            Description = request.Description?.Trim(),
            Status = ReportStatus.Pending
        };

        await reports.InsertOneAsync(report, cancellationToken: ct);

        return Result.Success();
    }
}
