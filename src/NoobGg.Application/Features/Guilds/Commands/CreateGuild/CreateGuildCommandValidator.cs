using FluentValidation;

namespace NoobGg.Application.Features.Guilds.Commands.CreateGuild;

public class CreateGuildCommandValidator : AbstractValidator<CreateGuildCommand>
{
    public CreateGuildCommandValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Guild name is required")
            .MinimumLength(3).WithMessage("Guild name must be at least 3 characters")
            .MaximumLength(50).WithMessage("Guild name must not exceed 50 characters");

        RuleFor(x => x.Tag)
            .NotEmpty().WithMessage("Guild tag is required")
            .MinimumLength(2).WithMessage("Guild tag must be at least 2 characters")
            .MaximumLength(6).WithMessage("Guild tag must not exceed 6 characters")
            .Matches("^[A-Za-z0-9]+$").WithMessage("Guild tag must contain only letters and numbers");

        RuleFor(x => x.Description)
            .MaximumLength(500).WithMessage("Description must not exceed 500 characters")
            .When(x => x.Description is not null);

        RuleFor(x => x.GameIds)
            .Must(g => g.Count <= 10).WithMessage("Maximum 10 games allowed");

        RuleFor(x => x.Region)
            .IsInEnum().WithMessage("Invalid region");

        RuleFor(x => x.Language)
            .IsInEnum().WithMessage("Invalid language");
    }
}
