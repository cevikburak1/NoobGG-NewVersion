using MediatR;
using NoobGg.Application.Common.Models;

namespace NoobGg.Application.Features.Blocks.Commands.BlockUser;

public record BlockUserCommand : IRequest<Result>
{
    public string BlockedUserId { get; init; } = string.Empty;
}
