using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NoobGg.Application.Features.Community.Commands.AddComment;
using NoobGg.Application.Features.Community.Commands.CreatePost;
using NoobGg.Application.Features.Community.Commands.ToggleVote;
using NoobGg.Application.Features.Community.Queries.GetBoards;
using NoobGg.Application.Features.Community.Queries.GetComments;
using NoobGg.Application.Features.Community.Queries.GetFeed;
using NoobGg.Application.Features.Community.Queries.GetTopicDetail;
using NoobGg.Application.Features.Community.Queries.GetTopics;

namespace NoobGg.Api.Controllers;

[Authorize]
[Route("api/community")]
public class CommunityController : ApiControllerBase
{
    [HttpGet("boards")]
    public async Task<IActionResult> GetBoards()
    {
        var result = await Mediator.Send(new GetCommunityBoardsQuery());
        return HandleResult(result);
    }

    [HttpGet("topics")]
    public async Task<IActionResult> GetTopics(
        [FromQuery] string board = "general",
        [FromQuery] string sort = "latest",
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        var result = await Mediator.Send(new GetCommunityTopicsQuery
        {
            BoardSlug = board,
            Sort = sort,
            Page = Math.Clamp(page, 1, 1000),
            PageSize = Math.Clamp(pageSize, 1, 50)
        });

        return HandleResult(result);
    }

    [HttpGet("topics/{topicId}")]
    public async Task<IActionResult> GetTopicDetail(string topicId)
    {
        var result = await Mediator.Send(new GetCommunityTopicDetailQuery { TopicId = topicId });
        return HandleResult(result);
    }

    [HttpGet("feed/{gameId}")]
    public async Task<IActionResult> GetFeed(string gameId, [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        var result = await Mediator.Send(new GetCommunityFeedQuery { GameId = gameId, Page = page, PageSize = pageSize });
        return HandleResult(result);
    }

    [HttpPost("posts")]
    public async Task<IActionResult> CreatePost([FromBody] CreateCommunityPostCommand command)
    {
        var result = await Mediator.Send(command);
        return HandleResult(result);
    }

    [HttpPost("topics")]
    public async Task<IActionResult> CreateTopic([FromBody] CreateCommunityPostCommand command)
    {
        var result = await Mediator.Send(command);
        return HandleResult(result);
    }

    [HttpGet("posts/{postId}/comments")]
    public async Task<IActionResult> GetComments(string postId, [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        var result = await Mediator.Send(new GetPostCommentsQuery { PostId = postId, Page = page, PageSize = pageSize });
        return HandleResult(result);
    }

    [HttpPost("posts/{postId}/comments")]
    public async Task<IActionResult> AddComment(string postId, [FromBody] AddCommunityCommentCommand command)
    {
        var result = await Mediator.Send(command with { PostId = postId });
        return HandleResult(result);
    }

    [HttpGet("topics/{topicId}/comments")]
    public async Task<IActionResult> GetTopicComments(string topicId, [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        var result = await Mediator.Send(new GetPostCommentsQuery { PostId = topicId, Page = page, PageSize = pageSize });
        return HandleResult(result);
    }

    [HttpPost("topics/{topicId}/comments")]
    public async Task<IActionResult> AddTopicComment(string topicId, [FromBody] AddCommunityCommentCommand command)
    {
        var result = await Mediator.Send(command with { PostId = topicId });
        return HandleResult(result);
    }

    [HttpPost("votes")]
    public async Task<IActionResult> ToggleVote([FromBody] ToggleContentVoteCommand command)
    {
        var result = await Mediator.Send(command);
        return HandleResult(result);
    }
}
