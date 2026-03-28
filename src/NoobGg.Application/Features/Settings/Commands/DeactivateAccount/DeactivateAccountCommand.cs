using MediatR;
using NoobGg.Application.Common.Models;

namespace NoobGg.Application.Features.Settings.Commands.DeactivateAccount;

public record DeactivateAccountCommand : IRequest<Result>
{
    public string? Reason { get; init; }
}
