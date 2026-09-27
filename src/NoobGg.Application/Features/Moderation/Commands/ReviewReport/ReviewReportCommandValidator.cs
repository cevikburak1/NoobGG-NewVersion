using FluentValidation;

namespace NoobGg.Application.Features.Moderation.Commands.ReviewReport;

public class ReviewReportCommandValidator : AbstractValidator<ReviewReportCommand>
{
    public ReviewReportCommandValidator()
    {
        RuleFor(x => x.ReportId)
            .NotEmpty().WithMessage("ReportId is required");

        RuleFor(x => x.Action)
            .Must(a => a is "warn" or "suspend" or "ban" or "dismiss")
            .WithMessage("Action must be warn, suspend, ban, or dismiss");

        RuleFor(x => x.Reason)
            .NotEmpty().When(x => x.Action is "warn" or "suspend" or "ban")
            .MaximumLength(2000);

        RuleFor(x => x.EndsAt)
            .NotNull().GreaterThan(DateTime.UtcNow).When(x => x.Action == "suspend");
    }
}
