using MediatR;
using NoobGg.Application.Common.Models;
using NoobGg.Application.Features.Reports.DTOs;
using NoobGg.Domain.Enums;

namespace NoobGg.Application.Features.Moderation.Queries.GetReports;

public record GetReportsQuery : IRequest<Result<PagedResult<ReportResponse>>>
{
    public ReportStatus? Status { get; init; }
    public ReportTargetType? TargetType { get; init; }
    public ReportReason? Reason { get; init; }
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 20;
}
