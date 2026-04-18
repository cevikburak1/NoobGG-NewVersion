using FluentValidation;

namespace NoobGg.Application.Features.Community.Commands.CreateBoard;

public class CreateCommunityBoardCommandValidator : AbstractValidator<CreateCommunityBoardCommand>
{
    public CreateCommunityBoardCommandValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Board name is required")
            .MaximumLength(80).WithMessage("Board name cannot exceed 80 characters");

        RuleFor(x => x.Description)
            .NotEmpty().WithMessage("Board description is required")
            .MaximumLength(280).WithMessage("Board description cannot exceed 280 characters");

        RuleFor(x => x.Category)
            .NotEmpty().WithMessage("Category is required")
            .MaximumLength(40).WithMessage("Category cannot exceed 40 characters");

        RuleFor(x => x.Slug)
            .MaximumLength(80).WithMessage("Slug cannot exceed 80 characters")
            .When(x => !string.IsNullOrWhiteSpace(x.Slug));

        RuleFor(x => x.CoverImageUrl)
            .MaximumLength(600).WithMessage("Cover image URL cannot exceed 600 characters")
            .When(x => !string.IsNullOrWhiteSpace(x.CoverImageUrl));
    }
}
