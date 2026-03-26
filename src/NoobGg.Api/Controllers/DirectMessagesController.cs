using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NoobGg.Application.Features.DirectMessages.Commands.CreateConversation;
using NoobGg.Application.Features.DirectMessages.Commands.MarkConversationRead;
using NoobGg.Application.Features.DirectMessages.Commands.SendDirectMessage;
using NoobGg.Application.Features.DirectMessages.Queries.GetConversations;
using NoobGg.Application.Features.DirectMessages.Queries.GetMessages;

namespace NoobGg.Api.Controllers;

[Route("api/dm")]
[Authorize]
public class DirectMessagesController : ApiControllerBase
{
    [HttpGet("conversations")]
    public async Task<IActionResult> GetConversations()
    {
        var result = await Mediator.Send(new GetConversationsQuery());
        return HandleResult(result);
    }

    [HttpPost("conversations")]
    public async Task<IActionResult> CreateConversation([FromBody] CreateConversationCommand command)
    {
        var result = await Mediator.Send(command);
        return HandleResult(result);
    }

    [HttpGet("conversations/{conversationId}/messages")]
    public async Task<IActionResult> GetMessages(
        string conversationId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50)
    {
        var result = await Mediator.Send(new GetMessagesQuery
        {
            ConversationId = conversationId,
            Page = page,
            PageSize = pageSize
        });
        return HandleResult(result);
    }

    [HttpPost("conversations/{conversationId}/messages")]
    public async Task<IActionResult> SendMessage(
        string conversationId,
        [FromBody] SendDirectMessageCommand command)
    {
        var updated = command with { ConversationId = conversationId };
        var result = await Mediator.Send(updated);
        return HandleResult(result);
    }

    [HttpPost("conversations/{conversationId}/read")]
    public async Task<IActionResult> MarkRead(string conversationId)
    {
        var result = await Mediator.Send(new MarkConversationReadCommand
        {
            ConversationId = conversationId
        });
        return HandleResult(result);
    }
}
