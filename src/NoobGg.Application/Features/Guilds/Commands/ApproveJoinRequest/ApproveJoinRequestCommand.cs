using MediatR;
using NoobGg.Application.Common.Models;

namespace NoobGg.Application.Features.Guilds.Commands.ApproveJoinRequest;

public record ApproveJoinRequestCommand : IRequest<Result>
{
    public string JoinRequestId { get; init; } = string.Empty;
}
