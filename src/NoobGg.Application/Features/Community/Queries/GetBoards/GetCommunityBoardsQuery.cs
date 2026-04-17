using MediatR;
using NoobGg.Application.Common.Models;
using NoobGg.Application.Features.Community.DTOs;

namespace NoobGg.Application.Features.Community.Queries.GetBoards;

public record GetCommunityBoardsQuery : IRequest<Result<CommunityBoardsOverviewResponse>>
{
}
