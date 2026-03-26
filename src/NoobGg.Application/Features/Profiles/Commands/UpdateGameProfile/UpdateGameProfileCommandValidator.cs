using FluentValidation;

namespace NoobGg.Application.Features.Profiles.Commands.UpdateGameProfile;

public class UpdateGameProfileCommandValidator : AbstractValidator<UpdateGameProfileCommand>
{
    public UpdateGameProfileCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.Rank).MaximumLength(50).When(x => x.Rank is not null);
        RuleFor(x => x.Role).MaximumLength(50).When(x => x.Role is not null);
        RuleFor(x => x.Note).MaximumLength(300).When(x => x.Note is not null);
        RuleFor(x => x.InGameName).MaximumLength(100).When(x => x.InGameName is not null);
    }
}
