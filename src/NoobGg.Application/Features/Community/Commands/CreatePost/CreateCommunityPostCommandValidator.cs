using FluentValidation;
using NoobGg.Domain.Enums;

namespace NoobGg.Application.Features.Community.Commands.CreatePost;

public class CreateCommunityPostCommandValidator : AbstractValidator<CreateCommunityPostCommand>
{
    public CreateCommunityPostCommandValidator()
    {
        RuleFor(x => x.Content)
            .NotEmpty().WithMessage("Content is required")
            .MaximumLength(1000).WithMessage("Content cannot exceed 1000 characters");

        RuleFor(x => x.Title)
            .MaximumLength(140).WithMessage("Title cannot exceed 140 characters");

        RuleFor(x => x.Category)
            .NotEmpty().WithMessage("Category is required")
            .MaximumLength(40).WithMessage("Category cannot exceed 40 characters");

        RuleFor(x => x.GameId)
            .NotEmpty().WithMessage("GameId is required for game boards")
            .When(x => x.BoardType == CommunityBoardType.Game);
    }
}
