using FluentValidation;

namespace NoobGg.Application.Features.Blocks.Commands.UnblockUser;

public class UnblockUserCommandValidator : AbstractValidator<UnblockUserCommand>
{
    public UnblockUserCommandValidator()
    {
        RuleFor(x => x.BlockedUserId)
            .NotEmpty().WithMessage("BlockedUserId is required");
    }
}
