using MediatR;
using NoobGg.Application.Common.Models;

namespace NoobGg.Application.Features.Settings.Commands.ReactivateAccount;

public record ReactivateAccountCommand : IRequest<Result>;
