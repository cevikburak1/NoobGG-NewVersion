using FluentValidation;
using NoobGg.Domain.Enums;

namespace NoobGg.Application.Features.Settings.Commands.UpdatePrivacySettings;

public class UpdatePrivacySettingsCommandValidator : AbstractValidator<UpdatePrivacySettingsCommand>
{
    public UpdatePrivacySettingsCommandValidator()
    {
        RuleFor(x => x.ProfileVisibility)
            .IsInEnum()
            .WithMessage("Invalid profile visibility value.");

        RuleFor(x => x.DmPermission)
            .IsInEnum()
            .WithMessage("Invalid DM permission value.");
    }
}
