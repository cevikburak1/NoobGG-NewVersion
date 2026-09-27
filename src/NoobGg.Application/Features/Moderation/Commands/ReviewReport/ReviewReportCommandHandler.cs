using MediatR;
using MongoDB.Driver;
using NoobGg.Application.Common.Constants;
using NoobGg.Application.Common.Interfaces;
using NoobGg.Application.Common.Models;
using NoobGg.Domain.Entities;
using NoobGg.Domain.Enums;

namespace NoobGg.Application.Features.Moderation.Commands.ReviewReport;

public class ReviewReportCommandHandler : IRequestHandler<ReviewReportCommand, Result>
{
    private readonly IMongoContext _mongoContext;
    private readonly ICurrentUser _currentUser;
    private readonly INotificationService _notificationService;

    public ReviewReportCommandHandler(
        IMongoContext mongoContext,
        ICurrentUser currentUser,
        INotificationService notificationService)
    {
        _mongoContext = mongoContext;
        _currentUser = currentUser;
        _notificationService = notificationService;
    }

    public async Task<Result> Handle(ReviewReportCommand request, CancellationToken ct)
    {
        if (!_currentUser.IsAuthenticated || _currentUser.UserId is null)
            return Result.Fail("Unauthorized", 401);

        if (_currentUser.Role is not ("Moderator" or "Admin"))
            return Result.Fail("Forbidden", 403);

        var reports = _mongoContext.GetCollection<Report>(CollectionNames.Reports);
        var report = await reports.Find(r => r.Id == request.ReportId).FirstOrDefaultAsync(ct);

        if (report is null)
            return Result.Fail("Report not found", 404);

        if (report.Status is ReportStatus.Resolved or ReportStatus.Dismissed)
            return Result.Fail("Report has already been closed", 409);

        var action = request.Action;
        var targetId = report.ReportedUserId;
        var sanctionsUser = action is "warn" or "suspend" or "ban";
        if (sanctionsUser && string.IsNullOrWhiteSpace(targetId))
            return Result.Fail("This report has no user to sanction", 400);

        if (sanctionsUser && targetId == _currentUser.UserId)
            return Result.Fail("You cannot sanction your own account", 400);

        var users = _mongoContext.GetCollection<User>(CollectionNames.Users);
        User? target = null;
        if (sanctionsUser)
        {
            target = await users.Find(u => u.Id == targetId).FirstOrDefaultAsync(ct);
            if (target is null)
                return Result.Fail("Reported user not found", 404);
            if (target.Role == UserRole.Admin ||
                (target.Role == UserRole.Moderator && _currentUser.Role != "Admin"))
                return Result.Fail("Insufficient role to sanction this user", 403);
        }

        var now = DateTime.UtcNow;
        var status = action switch
        {
            "warn" => ReportStatus.Resolved,
            "dismiss" => ReportStatus.Dismissed,
            _ => ReportStatus.Resolved
        };
        var update = Builders<Report>.Update
            .Set(r => r.Status, status)
            .Set(r => r.ReviewedBy, _currentUser.UserId)
            .Set(r => r.ReviewNote, request.Reason?.Trim())
            .Set(r => r.ReviewedAt, now)
            .Set(r => r.UpdatedAt, now);

        var updated = await reports.UpdateOneAsync(
            r => r.Id == request.ReportId &&
                 (r.Status == ReportStatus.Pending || r.Status == ReportStatus.Reviewed),
            update, cancellationToken: ct);
        if (updated.ModifiedCount == 0)
            return Result.Fail("Report has already been closed", 409);

        if (action is "ban" or "suspend")
        {
            var userUpdate = action == "ban"
                ? Builders<User>.Update.Set(u => u.IsBanned, true)
                    .Set(u => u.BanReason, request.Reason?.Trim())
                    .Set(u => u.BannedAt, now)
                : Builders<User>.Update.Set(u => u.SuspendedUntil, request.EndsAt)
                    .Set(u => u.SuspensionReason, request.Reason?.Trim());
            await users.UpdateOneAsync(u => u.Id == targetId, userUpdate.Set(u => u.UpdatedAt, now), cancellationToken: ct);

            var tokens = _mongoContext.GetCollection<RefreshToken>(CollectionNames.RefreshTokens);
            await tokens.UpdateManyAsync(t => t.UserId == targetId && t.RevokedAt == null,
                Builders<RefreshToken>.Update.Set(t => t.RevokedAt, now), cancellationToken: ct);
            await _notificationService.ForceDisconnectAsync(targetId,
                action == "ban" ? "Account banned" : "Account suspended", ct);
        }

        var auditAction = action switch
        {
            "dismiss" => AuditAction.ReportDismissed,
            "warn" => AuditAction.UserWarned,
            "suspend" => AuditAction.UserSuspended,
            "ban" => AuditAction.UserBanned,
            _ => AuditAction.ReportReviewed
        };

        var audits = _mongoContext.GetCollection<Audit>(CollectionNames.Audits);
        await audits.InsertOneAsync(new Audit
        {
            ActorId = _currentUser.UserId,
            Action = auditAction,
            TargetType = sanctionsUser ? "User" : "Report",
            TargetId = sanctionsUser ? targetId : request.ReportId,
            Details = $"Report {request.ReportId}; action {action}; ends {request.EndsAt:O}; reason {request.Reason ?? "(none)"}"
        }, cancellationToken: ct);

        var statusText = action == "dismiss" ? "dismissed" : "reviewed";
        await _notificationService.CreateAsync(
            report.ReporterId,
            NotificationType.ReportResolved,
            $"Your report has been {statusText}",
            $"A moderator has {statusText} your report.",
            new Dictionary<string, string> { { "reportId", request.ReportId } },
            ct);

        if (sanctionsUser)
        {
            await _notificationService.CreateAsync(targetId, NotificationType.SystemMessage,
                $"Moderation action: {action}", request.Reason?.Trim() ?? action,
                new Dictionary<string, string> { { "reportId", request.ReportId } }, ct);
        }

        return Result.Success();
    }
}
