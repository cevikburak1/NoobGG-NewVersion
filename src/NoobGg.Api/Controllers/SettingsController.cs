using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NoobGg.Application.Features.Settings.Commands.DeactivateAccount;
using NoobGg.Application.Features.Settings.Commands.ReactivateAccount;
using NoobGg.Application.Features.Settings.Commands.RequestAccountDeletion;
using NoobGg.Application.Features.Settings.Commands.UpdateNotificationSettings;
using NoobGg.Application.Features.Settings.Commands.UpdatePrivacySettings;
using NoobGg.Application.Features.Settings.Queries.GetMySettings;

namespace NoobGg.Api.Controllers;

[Route("api/settings")]
[Authorize]
public class SettingsController : ApiControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetMySettings()
    {
        var result = await Mediator.Send(new GetMySettingsQuery());
        return HandleResult(result);
    }

    [HttpPut("privacy")]
    public async Task<IActionResult> UpdatePrivacy([FromBody] UpdatePrivacySettingsCommand command)
    {
        var result = await Mediator.Send(command);
        return HandleResult(result);
    }

    [HttpPut("notifications")]
    public async Task<IActionResult> UpdateNotifications([FromBody] UpdateNotificationSettingsCommand command)
    {
        var result = await Mediator.Send(command);
        return HandleResult(result);
    }

    [HttpPost("deactivate")]
    public async Task<IActionResult> Deactivate([FromBody] DeactivateAccountCommand command)
    {
        var result = await Mediator.Send(command);
        return HandleResult(result);
    }

    [HttpPost("reactivate")]
    public async Task<IActionResult> Reactivate()
    {
        var result = await Mediator.Send(new ReactivateAccountCommand());
        return HandleResult(result);
    }

    [HttpPost("request-deletion")]
    public async Task<IActionResult> RequestDeletion()
    {
        var result = await Mediator.Send(new RequestAccountDeletionCommand());
        return HandleResult(result);
    }
}
