using MediatR;
using NoobGg.Application.Common.Models;

namespace NoobGg.Application.Features.Auth.Commands.Logout;

public record LogoutCommand : IRequest<Result>;
