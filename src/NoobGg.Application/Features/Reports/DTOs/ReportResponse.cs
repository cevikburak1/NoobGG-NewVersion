namespace NoobGg.Application.Features.Reports.DTOs;

public record ReportResponse(
    string Id,
    string TargetType,
    string ReportedUserId,
    string? ReportedUsername,
    string? RoomId,
    string? RoomTitle,
    string Reason,
    string? Description,
    string Status,
    DateTime CreatedAt);

public record ReportDetailResponse(
    string Id,
    string ReporterId,
    string? ReporterUsername,
    string TargetType,
    string ReportedUserId,
    string? ReportedUsername,
    string? RoomId,
    string? RoomTitle,
    string Reason,
    string? Description,
    string Status,
    string? ReviewedBy,
    string? ReviewerUsername,
    string? ReviewNote,
    DateTime? ReviewedAt,
    DateTime CreatedAt);
