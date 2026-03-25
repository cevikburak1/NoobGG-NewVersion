using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NoobGg.Application.Features.Rooms.Commands.CloseRoom;
using NoobGg.Application.Features.Rooms.Commands.CreateRoom;
using NoobGg.Application.Features.Rooms.Commands.JoinRoom;
using NoobGg.Application.Features.Rooms.Commands.KickMember;
using NoobGg.Application.Features.Rooms.Commands.LeaveRoom;
using NoobGg.Application.Features.Rooms.Queries.GetRoomDetails;
using NoobGg.Application.Features.Rooms.Queries.GetRooms;
using NoobGg.Domain.Enums;

namespace NoobGg.Api.Controllers;

[Route("api/rooms")]
[Authorize]
public class RoomsController : ApiControllerBase
{
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateRoomCommand command)
    {
        var result = await Mediator.Send(command);
        return HandleResult(result);
    }

    [HttpGet]
    [AllowAnonymous]
    public async Task<IActionResult> GetRooms(
        [FromQuery] string? gameId,
        [FromQuery] Region? region,
        [FromQuery] Language? language,
        [FromQuery] string? tag,
        [FromQuery] RoomStatus? status,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        var query = new GetRoomsQuery
        {
            GameId = gameId,
            Region = region,
            Language = language,
            Tag = tag,
            Status = status,
            Page = page,
            PageSize = pageSize
        };

        var result = await Mediator.Send(query);
        return HandleResult(result);
    }

    [HttpGet("{id}")]
    [AllowAnonymous]
    public async Task<IActionResult> GetRoomDetails(string id)
    {
        var result = await Mediator.Send(new GetRoomDetailsQuery { RoomId = id });
        return HandleResult(result);
    }

    [HttpPost("{id}/join")]
    public async Task<IActionResult> Join(string id)
    {
        var result = await Mediator.Send(new JoinRoomCommand { RoomId = id });
        return HandleResult(result);
    }

    [HttpPost("{id}/leave")]
    public async Task<IActionResult> Leave(string id)
    {
        var result = await Mediator.Send(new LeaveRoomCommand { RoomId = id });
        return HandleResult(result);
    }

    [HttpPost("{id}/kick")]
    public async Task<IActionResult> Kick(string id, [FromBody] KickMemberRequest request)
    {
        var command = new KickMemberCommand { RoomId = id, UserId = request.UserId };
        var result = await Mediator.Send(command);
        return HandleResult(result);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Close(string id)
    {
        var result = await Mediator.Send(new CloseRoomCommand { RoomId = id });
        return HandleResult(result);
    }
}

public record KickMemberRequest(string UserId);
