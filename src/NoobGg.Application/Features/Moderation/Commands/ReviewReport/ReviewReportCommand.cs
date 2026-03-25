using MediatR;
using NoobGg.Application.Common.Models;
using NoobGg.Domain.Enums;

namespace NoobGg.Application.Features.Moderation.Commands.ReviewReport;

public record ReviewReportCommand : IRequest<Result>
{
    public string ReportId { get; init; } = string.Empty;
    public ReportStatus NewStatus { get; init; }
    public string? ReviewNote { get; init; }
}
