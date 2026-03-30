using FluentValidation;

namespace NoobGg.Application.Features.Guilds.Commands.JoinGuild;

public class JoinGuildCommandValidator : AbstractValidator<JoinGuildCommand>
{
    public JoinGuildCommandValidator()
    {
        RuleFor(x => x.GuildId).NotEmpty().WithMessage("GuildId is required");
        RuleFor(x => x.Message).MaximumLength(200).When(x => x.Message is not null);
    }
}
