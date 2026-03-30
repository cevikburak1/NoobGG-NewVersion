using FluentValidation;

namespace NoobGg.Application.Features.Guilds.Commands.DeclineGuildInvite;

public class DeclineGuildInviteCommandValidator : AbstractValidator<DeclineGuildInviteCommand>
{
    public DeclineGuildInviteCommandValidator()
    {
        RuleFor(x => x.InviteId).NotEmpty();
    }
}
