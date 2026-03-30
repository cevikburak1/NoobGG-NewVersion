using FluentValidation;

namespace NoobGg.Application.Features.Guilds.Commands.LeaveGuild;

public class LeaveGuildCommandValidator : AbstractValidator<LeaveGuildCommand>
{
    public LeaveGuildCommandValidator()
    {
        RuleFor(x => x.GuildId).NotEmpty().WithMessage("GuildId is required");
    }
}
