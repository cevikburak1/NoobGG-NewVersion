using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NoobGg.Application.Features.Auth.Commands.Login;
using NoobGg.Application.Features.Auth.Commands.Logout;
using NoobGg.Application.Features.Auth.Commands.RefreshToken;
using NoobGg.Application.Features.Auth.Commands.Register;
using NoobGg.Application.Features.Auth.Queries.GetCurrentUser;

namespace NoobGg.Api.Controllers;

[Route("api/auth")]
public class AuthController : ApiControllerBase
{
    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterCommand command)
    {
        var result = await Mediator.Send(command with { IpAddress = ClientIpAddress });
        return HandleResult(result);
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginCommand command)
    {
        var result = await Mediator.Send(command with { IpAddress = ClientIpAddress });
        return HandleResult(result);
    }

    [HttpPost("refresh")]
    public async Task<IActionResult> Refresh([FromBody] RefreshTokenCommand command)
    {
        var result = await Mediator.Send(command with { IpAddress = ClientIpAddress });
        return HandleResult(result);
    }

    [HttpPost("logout")]
    [Authorize]
    public async Task<IActionResult> Logout()
    {
        var result = await Mediator.Send(new LogoutCommand());
        return HandleResult(result);
    }

    [HttpGet("me")]
    [Authorize]
    public async Task<IActionResult> GetMe()
    {
        var result = await Mediator.Send(new GetCurrentUserQuery());
        return HandleResult(result);
    }
}
