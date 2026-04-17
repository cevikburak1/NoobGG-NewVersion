using FluentValidation;

namespace NoobGg.Application.Features.Guides.Commands.CreateGuide;

public class CreateGuideCommandValidator : AbstractValidator<CreateGuideCommand>
{
    public CreateGuideCommandValidator()
    {
        RuleFor(x => x.GameId)
            .NotEmpty().WithMessage("GameId is required");

        RuleFor(x => x.Title)
            .NotEmpty().WithMessage("Title is required")
            .MaximumLength(200).WithMessage("Title must not exceed 200 characters");

        RuleFor(x => x.Content)
            .NotEmpty().WithMessage("Content is required")
            .MaximumLength(50000).WithMessage("Content must not exceed 50000 characters");
    }
}
