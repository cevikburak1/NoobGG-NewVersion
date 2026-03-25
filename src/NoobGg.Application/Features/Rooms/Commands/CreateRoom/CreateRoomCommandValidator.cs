using FluentValidation;

namespace NoobGg.Application.Features.Rooms.Commands.CreateRoom;

public class CreateRoomCommandValidator : AbstractValidator<CreateRoomCommand>
{
    public CreateRoomCommandValidator()
    {
        RuleFor(x => x.Title)
            .NotEmpty().WithMessage("Title is required")
            .MinimumLength(3).WithMessage("Title must be at least 3 characters")
            .MaximumLength(100).WithMessage("Title must not exceed 100 characters");

        RuleFor(x => x.GameId)
            .NotEmpty().WithMessage("GameId is required");

        RuleFor(x => x.Description)
            .MaximumLength(500).WithMessage("Description must not exceed 500 characters")
            .When(x => x.Description is not null);

        RuleFor(x => x.Tags)
            .Must(t => t.Count <= 5).WithMessage("Maximum 5 tags allowed")
            .Must(t => t.All(tag => tag.Length <= 30)).WithMessage("Each tag must not exceed 30 characters");

        RuleFor(x => x.Region)
            .IsInEnum().WithMessage("Invalid region");

        RuleFor(x => x.Language)
            .IsInEnum().WithMessage("Invalid language");
    }
}
