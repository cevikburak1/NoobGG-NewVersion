using MediatR;
using NoobGg.Application.Common.Models;
using NoobGg.Application.Features.Blocks.DTOs;

namespace NoobGg.Application.Features.Blocks.Queries.GetBlockedUsers;

public record GetBlockedUsersQuery : IRequest<Result<List<BlockedUserResponse>>>;
