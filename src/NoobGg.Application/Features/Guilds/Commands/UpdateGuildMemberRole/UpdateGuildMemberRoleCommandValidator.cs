using FluentValidation;

namespace NoobGg.Application.Features.Guilds.Commands.UpdateGuildMemberRole;

public class UpdateGuildMemberRoleCommandValidator : AbstractValidator<UpdateGuildMemberRoleCommand>
{
    public UpdateGuildMemberRoleCommandValidator()
    {
        RuleFor(x => x.GuildId).NotEmpty();
        RuleFor(x => x.UserId).NotEmpty();
        RuleFor(x => x.NewRole).IsInEnum().WithMessage("Invalid role");
    }
}
