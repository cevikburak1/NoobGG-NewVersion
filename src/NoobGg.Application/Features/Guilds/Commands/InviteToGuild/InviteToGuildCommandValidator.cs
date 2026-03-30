using FluentValidation;

namespace NoobGg.Application.Features.Guilds.Commands.InviteToGuild;

public class InviteToGuildCommandValidator : AbstractValidator<InviteToGuildCommand>
{
    public InviteToGuildCommandValidator()
    {
        RuleFor(x => x.GuildId).NotEmpty();
        RuleFor(x => x.InvitedUserId).NotEmpty();
    }
}
