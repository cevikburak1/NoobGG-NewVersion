using MediatR;
using NoobGg.Application.Common.Models;
using NoobGg.Application.Features.Rooms.DTOs;

namespace NoobGg.Application.Features.Rooms.Queries.GetPendingInvites;

public record GetPendingInvitesQuery : IRequest<Result<List<RoomInviteResponse>>>;
