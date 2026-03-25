using MediatR;
using Microsoft.AspNetCore.Mvc;
using NoobGg.Application.Common.Models;

namespace NoobGg.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public abstract class ApiControllerBase : ControllerBase
{
    private IMediator? _mediator;
    protected IMediator Mediator => _mediator ??= HttpContext.RequestServices.GetRequiredService<IMediator>();

    protected IActionResult HandleResult<T>(Result<T> result)
    {
        if (result.IsSuccess)
            return StatusCode(result.StatusCode, result.Data);

        return StatusCode(result.StatusCode, new { type = "Error", title = result.Error, status = result.StatusCode });
    }

    protected IActionResult HandleResult(Result result)
    {
        if (result.IsSuccess)
            return StatusCode(result.StatusCode);

        return StatusCode(result.StatusCode, new { type = "Error", title = result.Error, status = result.StatusCode });
    }

    protected string? ClientIpAddress =>
        HttpContext.Connection.RemoteIpAddress?.ToString();
}
