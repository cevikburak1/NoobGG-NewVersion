using MediatR;
using NoobGg.Application.Common.Models;
using NoobGg.Domain.Enums;

namespace NoobGg.Application.Features.Reports.Commands.CreateReport;

public record CreateReportCommand : IRequest<Result>
{
    public ReportTargetType TargetType { get; init; }
    public string? ReportedUserId { get; init; }
    public string? RoomId { get; init; }
    public ReportReason Reason { get; init; }
    public string? Description { get; init; }
}
