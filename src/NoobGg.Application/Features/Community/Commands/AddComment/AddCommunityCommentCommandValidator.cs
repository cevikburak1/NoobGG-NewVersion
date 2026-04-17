using FluentValidation;

namespace NoobGg.Application.Features.Community.Commands.AddComment;

public class AddCommunityCommentCommandValidator : AbstractValidator<AddCommunityCommentCommand>
{
    public AddCommunityCommentCommandValidator()
    {
        RuleFor(x => x.PostId)
            .NotEmpty().WithMessage("PostId is required");

        RuleFor(x => x.Content)
            .NotEmpty().WithMessage("Content is required")
            .MaximumLength(500).WithMessage("Content cannot exceed 500 characters");
    }
}
