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

        var now = DateTime.UtcNow;
        var update = Builders<Report>.Update
            .Set(r => r.Status, request.NewStatus)
            .Set(r => r.ReviewedBy, _currentUser.UserId)
            .Set(r => r.ReviewNote, request.ReviewNote?.Trim())
            .Set(r => r.ReviewedAt, now)
            .Set(r => r.UpdatedAt, now);

        await reports.UpdateOneAsync(r => r.Id == request.ReportId, update, cancellationToken: ct);

        var auditAction = request.NewStatus == ReportStatus.Dismissed
            ? AuditAction.ReportDismissed
            : AuditAction.ReportReviewed;

        var audits = _mongoContext.GetCollection<Audit>(CollectionNames.Audits);
        await audits.InsertOneAsync(new Audit
        {
            ActorId = _currentUser.UserId,
            Action = auditAction,
            TargetType = "Report",
            TargetId = request.ReportId,
            Details = $"Status → {request.NewStatus}. Note: {request.ReviewNote ?? "(none)"}"
        }, cancellationToken: ct);

        var statusText = request.NewStatus == ReportStatus.Dismissed ? "dismissed" : "resolved";
        await _notificationService.CreateAsync(
            report.ReporterId,
            NotificationType.ReportResolved,
            $"Your report has been {statusText}",
            request.ReviewNote?.Trim() ?? $"A moderator has reviewed and {statusText} your report.",
            new Dictionary<string, string> { { "reportId", request.ReportId } },
            ct);

        return Result.Success();
    }
}
