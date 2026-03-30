using MediatR;
using NoobGg.Application.Common.Models;
using NoobGg.Application.Features.Guilds.DTOs;

namespace NoobGg.Application.Features.Guilds.Queries.GetPendingGuildInvites;

public record GetPendingGuildInvitesQuery : IRequest<Result<List<GuildInviteResponse>>>;
