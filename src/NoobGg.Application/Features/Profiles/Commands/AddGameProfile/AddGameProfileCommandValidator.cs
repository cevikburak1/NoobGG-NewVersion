using FluentValidation;

namespace NoobGg.Application.Features.Profiles.Commands.AddGameProfile;

public class AddGameProfileCommandValidator : AbstractValidator<AddGameProfileCommand>
{
    public AddGameProfileCommandValidator()
    {
        RuleFor(x => x.GameId).NotEmpty();
        RuleFor(x => x.Rank).NotEmpty().MaximumLength(50);
        RuleFor(x => x.Role).MaximumLength(50).When(x => x.Role is not null);
        RuleFor(x => x.Region).IsInEnum();
        RuleFor(x => x.ExperienceLevel).IsInEnum();
        RuleFor(x => x.CommunicationPreference).IsInEnum();
        RuleFor(x => x.Note).MaximumLength(300).When(x => x.Note is not null);
        RuleFor(x => x.InGameName).MaximumLength(100).When(x => x.InGameName is not null);
    }
}
