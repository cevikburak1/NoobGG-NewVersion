using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NoobGg.Application.Features.Moderation.Commands.ReviewReport;
using NoobGg.Application.Features.Moderation.Queries.GetReportDetails;
using NoobGg.Application.Features.Moderation.Queries.GetReports;
using NoobGg.Domain.Enums;

namespace NoobGg.Api.Controllers;

[Route("api/moderation")]
[Authorize(Policy = "RequireModerator")]
public class ModerationController : ApiControllerBase
{
    [HttpGet("reports")]
    public async Task<IActionResult> GetReports(
        [FromQuery] ReportStatus? status,
        [FromQuery] ReportTargetType? targetType,
        [FromQuery] ReportReason? reason,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        var query = new GetReportsQuery
        {
            Status = status,
            TargetType = targetType,
            Reason = reason,
            Page = page,
            PageSize = pageSize
        };

        var result = await Mediator.Send(query);
        return HandleResult(result);
    }

    [HttpGet("reports/{id}")]
    public async Task<IActionResult> GetReportDetails(string id)
    {
        var result = await Mediator.Send(new GetReportDetailsQuery { ReportId = id });
        return HandleResult(result);
    }

    [HttpPost("reports/{id}/review")]
    public async Task<IActionResult> ReviewReport(string id, [FromBody] ReviewReportRequest request)
    {
        var command = new ReviewReportCommand
        {
            ReportId = id,
            NewStatus = request.Status,
            ReviewNote = request.ReviewNote
        };

        var result = await Mediator.Send(command);
        return HandleResult(result);
    }
}

public record ReviewReportRequest(ReportStatus Status, string? ReviewNote);
