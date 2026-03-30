using FluentValidation;

namespace NoobGg.Application.Features.Guilds.Commands.AcceptGuildInvite;

public class AcceptGuildInviteCommandValidator : AbstractValidator<AcceptGuildInviteCommand>
{
    public AcceptGuildInviteCommandValidator()
    {
        RuleFor(x => x.InviteId).NotEmpty();
    }
}
