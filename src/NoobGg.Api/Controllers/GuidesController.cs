using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NoobGg.Application.Features.Guides.Commands.CreateGuide;
using NoobGg.Application.Features.Guides.Queries.GetGuideDetail;
using NoobGg.Application.Features.Guides.Queries.GetGuides;

namespace NoobGg.Api.Controllers;

[Authorize]
[Route("api/guides")]
public class GuidesController : ApiControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetGuides(
        [FromQuery] string? gameId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? sortBy = "recent")
    {
        var result = await Mediator.Send(new GetGuidesQuery
        {
            GameId = gameId,
            Page = page,
            PageSize = pageSize,
            SortBy = sortBy
        });
        return HandleResult(result);
    }

    [HttpGet("{guideId}")]
    public async Task<IActionResult> GetGuideDetail(string guideId)
    {
        var result = await Mediator.Send(new GetGuideDetailQuery { GuideId = guideId });
        return HandleResult(result);
    }

    [HttpPost]
    public async Task<IActionResult> CreateGuide([FromBody] CreateGuideCommand command)
    {
        var result = await Mediator.Send(command);
        return HandleResult(result);
    }
}
