using MediatR;
using NoobGg.Application.Common.Models;
using NoobGg.Application.Features.Matchmaking.DTOs;

namespace NoobGg.Application.Features.Matchmaking.Queries.GetMatchQueueStatus;

public record GetMatchQueueStatusQuery : IRequest<Result<GetMatchQueueStatusResponse>>;
