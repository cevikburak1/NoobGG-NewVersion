using MediatR;
using NoobGg.Application.Common.Models;

namespace NoobGg.Application.Features.Moderation.Commands.ReviewReport;

public record ReviewReportCommand : IRequest<Result>
{
    public string ReportId { get; init; } = string.Empty;
    public string Action { get; init; } = string.Empty;
    public string? Reason { get; init; }
    public DateTime? EndsAt { get; init; }
}
