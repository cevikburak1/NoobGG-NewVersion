using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NoobGg.Application.Common.Interfaces;
using NoobGg.Application.Features.Profiles.Commands.AddGameProfile;
using NoobGg.Application.Features.Profiles.Commands.DeleteGameProfile;
using NoobGg.Application.Features.Profiles.Commands.UpdateGameProfile;
using NoobGg.Application.Features.Profiles.Commands.UpdateProfile;
using NoobGg.Application.Features.Profiles.Queries.GetGameProfiles;
using NoobGg.Application.Features.Profiles.Queries.GetProfile;

namespace NoobGg.Api.Controllers;

[Route("api/profiles")]
public class ProfilesController : ApiControllerBase
{
    [HttpGet("{userId}")]
    public async Task<IActionResult> GetProfile(string userId)
    {
        var result = await Mediator.Send(new GetProfileQuery { UserId = userId });
        return HandleResult(result);
    }

    [HttpGet("me")]
    [Authorize]
    public async Task<IActionResult> GetMyProfile([FromServices] ICurrentUser currentUser)
    {
        if (currentUser.UserId is null)
            return Unauthorized();

        var result = await Mediator.Send(new GetProfileQuery { UserId = currentUser.UserId });
        return HandleResult(result);
    }

    [HttpPut("me")]
    [Authorize]
    public async Task<IActionResult> UpdateProfile([FromBody] UpdateProfileCommand command)
    {
        var result = await Mediator.Send(command);
        return HandleResult(result);
    }

    [HttpGet("{userId}/games")]
    public async Task<IActionResult> GetGameProfiles(string userId)
    {
        var result = await Mediator.Send(new GetGameProfilesQuery { UserId = userId });
        return HandleResult(result);
    }

    [HttpPost("me/games")]
    [Authorize]
    public async Task<IActionResult> AddGameProfile([FromBody] AddGameProfileCommand command)
    {
        var result = await Mediator.Send(command);
        return HandleResult(result);
    }

    [HttpPut("me/games/{id}")]
    [Authorize]
    public async Task<IActionResult> UpdateGameProfile(string id, [FromBody] UpdateGameProfileCommand command)
    {
        var updated = command with { Id = id };
        var result = await Mediator.Send(updated);
        return HandleResult(result);
    }

    [HttpDelete("me/games/{id}")]
    [Authorize]
    public async Task<IActionResult> DeleteGameProfile(string id)
    {
        var result = await Mediator.Send(new DeleteGameProfileCommand { Id = id });
        return HandleResult(result);
    }
}
