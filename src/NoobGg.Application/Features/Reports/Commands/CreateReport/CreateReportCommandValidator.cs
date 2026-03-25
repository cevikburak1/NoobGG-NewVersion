using FluentValidation;
using NoobGg.Domain.Enums;

namespace NoobGg.Application.Features.Reports.Commands.CreateReport;

public class CreateReportCommandValidator : AbstractValidator<CreateReportCommand>
{
    public CreateReportCommandValidator()
    {
        RuleFor(x => x.TargetType)
            .IsInEnum().WithMessage("Invalid target type");

        RuleFor(x => x.ReportedUserId)
            .NotEmpty().When(x => x.TargetType == ReportTargetType.User)
            .WithMessage("ReportedUserId is required for user reports");

        RuleFor(x => x.RoomId)
            .NotEmpty().When(x => x.TargetType == ReportTargetType.Room)
            .WithMessage("RoomId is required for room reports");

        RuleFor(x => x.Reason)
            .IsInEnum().WithMessage("Invalid report reason");

        RuleFor(x => x.Description)
            .MaximumLength(1000).When(x => x.Description is not null)
            .WithMessage("Description cannot exceed 1000 characters");
    }
}
