using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NoobGg.Application.Features.Reports.Commands.CreateReport;

namespace NoobGg.Api.Controllers;

[Route("api/reports")]
[Authorize]
public class ReportsController : ApiControllerBase
{
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateReportCommand command)
    {
        var result = await Mediator.Send(command);
        return HandleResult(result);
    }
}
