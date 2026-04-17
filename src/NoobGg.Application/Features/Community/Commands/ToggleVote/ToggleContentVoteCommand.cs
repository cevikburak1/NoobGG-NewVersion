using MediatR;
using NoobGg.Application.Common.Models;
using NoobGg.Domain.Enums;

namespace NoobGg.Application.Features.Community.Commands.ToggleVote;

public record ToggleContentVoteCommand : IRequest<Result<bool>>
{
    public string TargetId { get; init; } = string.Empty;
    public ContentVoteTargetType TargetType { get; init; }
}
