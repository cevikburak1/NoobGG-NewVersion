using FluentValidation;

namespace NoobGg.Application.Features.Guilds.Commands.KickGuildMember;

public class KickGuildMemberCommandValidator : AbstractValidator<KickGuildMemberCommand>
{
    public KickGuildMemberCommandValidator()
    {
        RuleFor(x => x.GuildId).NotEmpty();
        RuleFor(x => x.UserId).NotEmpty();
    }
}
