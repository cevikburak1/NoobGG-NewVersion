using FluentValidation;
using NoobGg.Domain.Enums;

namespace NoobGg.Application.Features.Moderation.Commands.ReviewReport;

public class ReviewReportCommandValidator : AbstractValidator<ReviewReportCommand>
{
    public ReviewReportCommandValidator()
    {
        RuleFor(x => x.ReportId)
            .NotEmpty().WithMessage("ReportId is required");

        RuleFor(x => x.NewStatus)
            .Must(s => s is ReportStatus.Reviewed or ReportStatus.Resolved or ReportStatus.Dismissed)
            .WithMessage("Status must be Reviewed, Resolved, or Dismissed");

        RuleFor(x => x.ReviewNote)
            .MaximumLength(2000).When(x => x.ReviewNote is not null);
    }
}
