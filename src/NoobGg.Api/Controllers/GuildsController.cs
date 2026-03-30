using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NoobGg.Application.Features.Guilds.Commands.AcceptGuildInvite;
using NoobGg.Application.Features.Guilds.Commands.ApproveJoinRequest;
using NoobGg.Application.Features.Guilds.Commands.CreateGuild;
using NoobGg.Application.Features.Guilds.Commands.DeclineGuildInvite;
using NoobGg.Application.Features.Guilds.Commands.InviteToGuild;
using NoobGg.Application.Features.Guilds.Commands.JoinGuild;
using NoobGg.Application.Features.Guilds.Commands.KickGuildMember;
using NoobGg.Application.Features.Guilds.Commands.LeaveGuild;
using NoobGg.Application.Features.Guilds.Commands.RejectJoinRequest;
using NoobGg.Application.Features.Guilds.Commands.UpdateGuildMemberRole;
using NoobGg.Application.Features.Guilds.Queries.GetGuildDetail;
using NoobGg.Application.Features.Guilds.Queries.GetGuilds;
using NoobGg.Application.Features.Guilds.Queries.GetPendingGuildInvites;
using NoobGg.Application.Features.Guilds.Queries.GetPendingJoinRequests;
using NoobGg.Domain.Enums;

namespace NoobGg.Api.Controllers;

[Route("api/guilds")]
[Authorize]
public class GuildsController : ApiControllerBase
{
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateGuildCommand command)
    {
        var result = await Mediator.Send(command);
        return HandleResult(result);
    }

    [HttpGet]
    [AllowAnonymous]
    public async Task<IActionResult> GetGuilds(
        [FromQuery] string? gameId,
        [FromQuery] Region? region,
        [FromQuery] Language? language,
        [FromQuery] string? search,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        var query = new GetGuildsQuery
        {
            GameId = gameId,
            Region = region,
            Language = language,
            Search = search,
            Page = page,
            PageSize = pageSize
        };

        var result = await Mediator.Send(query);
        return HandleResult(result);
    }

    [HttpGet("{id}")]
    [AllowAnonymous]
    public async Task<IActionResult> GetGuildDetail(string id)
    {
        var result = await Mediator.Send(new GetGuildDetailQuery { GuildId = id });
        return HandleResult(result);
    }

    [HttpPost("{id}/join")]
    public async Task<IActionResult> Join(string id, [FromBody] JoinGuildRequest? body)
    {
        var result = await Mediator.Send(new JoinGuildCommand
        {
            GuildId = id,
            Message = body?.Message
        });
        return HandleResult(result);
    }

    [HttpPost("{id}/leave")]
    public async Task<IActionResult> Leave(string id)
    {
        var result = await Mediator.Send(new LeaveGuildCommand { GuildId = id });
        return HandleResult(result);
    }

    [HttpPost("{id}/kick")]
    public async Task<IActionResult> Kick(string id, [FromBody] KickGuildMemberRequest request)
    {
        var command = new KickGuildMemberCommand { GuildId = id, UserId = request.UserId };
        var result = await Mediator.Send(command);
        return HandleResult(result);
    }

    [HttpPost("{id}/role")]
    public async Task<IActionResult> UpdateRole(string id, [FromBody] UpdateRoleRequest request)
    {
        var command = new UpdateGuildMemberRoleCommand
        {
            GuildId = id,
            UserId = request.UserId,
            NewRole = request.NewRole
        };
        var result = await Mediator.Send(command);
        return HandleResult(result);
    }

    [HttpPost("{id}/invite/{userId}")]
    public async Task<IActionResult> Invite(string id, string userId)
    {
        var result = await Mediator.Send(new InviteToGuildCommand { GuildId = id, InvitedUserId = userId });
        return HandleResult(result);
    }

    [HttpGet("invites")]
    public async Task<IActionResult> GetPendingInvites()
    {
        var result = await Mediator.Send(new GetPendingGuildInvitesQuery());
        return HandleResult(result);
    }

    [HttpPost("invites/{inviteId}/accept")]
    public async Task<IActionResult> AcceptInvite(string inviteId)
    {
        var result = await Mediator.Send(new AcceptGuildInviteCommand { InviteId = inviteId });
        return HandleResult(result);
    }

    [HttpPost("invites/{inviteId}/decline")]
    public async Task<IActionResult> DeclineInvite(string inviteId)
    {
        var result = await Mediator.Send(new DeclineGuildInviteCommand { InviteId = inviteId });
        return HandleResult(result);
    }

    [HttpGet("{id}/join-requests")]
    public async Task<IActionResult> GetPendingJoinRequests(string id)
    {
        var result = await Mediator.Send(new GetPendingJoinRequestsQuery { GuildId = id });
        return HandleResult(result);
    }

    [HttpPost("join-requests/{joinRequestId}/approve")]
    public async Task<IActionResult> ApproveJoinRequest(string joinRequestId)
    {
        var result = await Mediator.Send(new ApproveJoinRequestCommand { JoinRequestId = joinRequestId });
        return HandleResult(result);
    }

    [HttpPost("join-requests/{joinRequestId}/reject")]
    public async Task<IActionResult> RejectJoinRequest(string joinRequestId)
    {
        var result = await Mediator.Send(new RejectJoinRequestCommand { JoinRequestId = joinRequestId });
        return HandleResult(result);
    }
}

public record JoinGuildRequest(string? Message);
public record KickGuildMemberRequest(string UserId);
public record UpdateRoleRequest(string UserId, GuildMemberRole NewRole);
