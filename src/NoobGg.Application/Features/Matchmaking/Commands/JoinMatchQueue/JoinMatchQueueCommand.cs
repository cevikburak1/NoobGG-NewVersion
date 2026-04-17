using MediatR;
using NoobGg.Application.Common.Models;
using NoobGg.Application.Features.Matchmaking.DTOs;
using NoobGg.Domain.Enums;

namespace NoobGg.Application.Features.Matchmaking.Commands.JoinMatchQueue;

public record JoinMatchQueueCommand : IRequest<Result<JoinMatchQueueResponse>>
{
    public string GameId { get; init; } = string.Empty;

    // Optional UI overrides; when null the handler falls back to the user's game profile values.
    public Region? Region { get; init; }
    public Language? Language { get; init; }
}
