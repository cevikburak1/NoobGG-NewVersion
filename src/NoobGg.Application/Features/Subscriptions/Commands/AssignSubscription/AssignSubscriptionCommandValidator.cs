using FluentValidation;

namespace NoobGg.Application.Features.Subscriptions.Commands.AssignSubscription;

public class AssignSubscriptionCommandValidator : AbstractValidator<AssignSubscriptionCommand>
{
    public AssignSubscriptionCommandValidator()
    {
        RuleFor(x => x.UserId)
            .NotEmpty().WithMessage("UserId is required");

        RuleFor(x => x.PlanId)
            .NotEmpty().WithMessage("PlanId is required");

        RuleFor(x => x.DurationMonths)
            .GreaterThan(0).When(x => x.DurationMonths.HasValue)
            .WithMessage("DurationMonths must be positive");
    }
}
