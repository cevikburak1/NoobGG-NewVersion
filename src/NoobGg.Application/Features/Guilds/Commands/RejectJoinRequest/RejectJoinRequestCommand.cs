using MediatR;
using NoobGg.Application.Common.Models;

namespace NoobGg.Application.Features.Guilds.Commands.RejectJoinRequest;

public record RejectJoinRequestCommand : IRequest<Result>
{
    public string JoinRequestId { get; init; } = string.Empty;
}
