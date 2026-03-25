using FluentValidation;

namespace NoobGg.Application.Features.Blocks.Commands.BlockUser;

public class BlockUserCommandValidator : AbstractValidator<BlockUserCommand>
{
    public BlockUserCommandValidator()
    {
        RuleFor(x => x.BlockedUserId)
            .NotEmpty().WithMessage("BlockedUserId is required");
    }
}
