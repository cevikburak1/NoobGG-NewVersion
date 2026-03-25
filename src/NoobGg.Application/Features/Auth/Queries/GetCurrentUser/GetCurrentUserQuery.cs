using MediatR;
using NoobGg.Application.Common.Models;
using NoobGg.Application.Features.Auth.DTOs;

namespace NoobGg.Application.Features.Auth.Queries.GetCurrentUser;

public record GetCurrentUserQuery : IRequest<Result<UserResponse>>;
