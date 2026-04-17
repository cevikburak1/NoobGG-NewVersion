using FluentValidation;

namespace NoobGg.Application.Features.GuildEvents.Commands.CreateEvent;

public class CreateGuildEventCommandValidator : AbstractValidator<CreateGuildEventCommand>
{
    public CreateGuildEventCommandValidator()
    {
        RuleFor(x => x.GuildId)
            .NotEmpty().WithMessage("GuildId is required");

        RuleFor(x => x.Title)
            .NotEmpty().WithMessage("Title is required")
            .MaximumLength(200).WithMessage("Title must not exceed 200 characters");

        RuleFor(x => x.StartsAt)
            .GreaterThan(DateTime.UtcNow).WithMessage("Start date must be in the future");

        RuleFor(x => x.EndsAt)
            .GreaterThan(x => x.StartsAt).WithMessage("End date must be after the start date");
    }
}
