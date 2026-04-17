using FluentValidation;

namespace NoobGg.Application.Features.Tournaments.Commands.CreateTournament;

public class CreateTournamentCommandValidator : AbstractValidator<CreateTournamentCommand>
{
    public CreateTournamentCommandValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Tournament name is required")
            .MaximumLength(100).WithMessage("Tournament name cannot exceed 100 characters");

        RuleFor(x => x.GameId)
            .NotEmpty().WithMessage("GameId is required");

        RuleFor(x => x.MaxParticipants)
            .InclusiveBetween(4, 128).WithMessage("MaxParticipants must be between 4 and 128")
            .Must(IsPowerOfTwo).WithMessage("MaxParticipants must be a power of 2");

        RuleFor(x => x.RegistrationDeadline)
            .GreaterThan(DateTime.UtcNow).WithMessage("Registration deadline must be in the future");

        RuleFor(x => x.StartsAt)
            .GreaterThan(x => x.RegistrationDeadline)
            .When(x => x.StartsAt.HasValue)
            .WithMessage("Start time must be after registration deadline");
    }

    private static bool IsPowerOfTwo(int value) => value > 0 && (value & (value - 1)) == 0;
}
