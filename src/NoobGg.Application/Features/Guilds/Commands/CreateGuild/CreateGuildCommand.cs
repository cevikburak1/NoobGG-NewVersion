using MediatR;
using NoobGg.Application.Common.Models;
using NoobGg.Application.Features.Guilds.DTOs;
using NoobGg.Domain.Enums;

namespace NoobGg.Application.Features.Guilds.Commands.CreateGuild;

public record CreateGuildCommand : IRequest<Result<GuildDetailResponse>>
{
    public string Name { get; init; } = string.Empty;
    public string Tag { get; init; } = string.Empty;
    public string? Description { get; init; }
    public bool IsPublic { get; init; } = true;
    public Region Region { get; init; }
    public Language Language { get; init; }
    public List<string> GameIds { get; init; } = [];
}
