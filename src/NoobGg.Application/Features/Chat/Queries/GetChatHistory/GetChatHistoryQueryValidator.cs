using FluentValidation;

namespace NoobGg.Application.Features.Chat.Queries.GetChatHistory;

public class GetChatHistoryQueryValidator : AbstractValidator<GetChatHistoryQuery>
{
    public GetChatHistoryQueryValidator()
    {
        RuleFor(x => x.RoomId)
            .NotEmpty().WithMessage("RoomId is required");

        RuleFor(x => x.Page)
            .GreaterThanOrEqualTo(1).WithMessage("Page must be at least 1");

        RuleFor(x => x.PageSize)
            .InclusiveBetween(1, 100).WithMessage("PageSize must be between 1 and 100");
    }
}
