using MediatR;
using NoobGg.Application.Common.Models;

namespace NoobGg.Application.Features.Blocks.Commands.UnblockUser;

public record UnblockUserCommand : IRequest<Result>
{
    public string BlockedUserId { get; init; } = string.Empty;
}
