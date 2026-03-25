using MediatR;
using NoobGg.Application.Common.Models;
using NoobGg.Application.Features.Reports.DTOs;

namespace NoobGg.Application.Features.Moderation.Queries.GetReportDetails;

public record GetReportDetailsQuery : IRequest<Result<ReportDetailResponse>>
{
    public string ReportId { get; init; } = string.Empty;
}
