using MediatR;
using MongoDB.Driver;
using NoobGg.Application.Common.Constants;
using NoobGg.Application.Common.Interfaces;
using NoobGg.Application.Common.Models;
using NoobGg.Domain.Entities;
using NoobGg.Domain.Enums;

namespace NoobGg.Application.Features.Blocks.Commands.UnblockUser;

public class UnblockUserCommandHandler : IRequestHandler<UnblockUserCommand, Result>
{
    private readonly IMongoContext _mongoContext;
    private readonly ICurrentUser _currentUser;
    private readonly INotificationService _notificationService;

    public UnblockUserCommandHandler(IMongoContext mongoContext, ICurrentUser currentUser, INotificationService notificationService)
    {
        _mongoContext = mongoContext;
        _currentUser = currentUser;
        _notificationService = notificationService;
    }

    public async Task<Result> Handle(UnblockUserCommand request, CancellationToken ct)
    {
        if (!_currentUser.IsAuthenticated || _currentUser.UserId is null)
            return Result.Fail("Unauthorized", 401);

        var userId = _currentUser.UserId;
        var blocks = _mongoContext.GetCollection<Block>(CollectionNames.Blocks);

        var result = await blocks.DeleteOneAsync(
            b => b.BlockerId == userId && b.BlockedUserId == request.BlockedUserId, ct);

        if (result.DeletedCount == 0)
            return Result.Fail("Block not found", 404);

        await _notificationService.SendBlockListChangedAsync(userId, request.BlockedUserId, false, ct);

        var audits = _mongoContext.GetCollection<Audit>(CollectionNames.Audits);
        await audits.InsertOneAsync(new Audit
        {
            ActorId = userId,
            Action = AuditAction.UserUnblocked,
            TargetType = "User",
            TargetId = request.BlockedUserId
        }, cancellationToken: ct);

        return Result.Success();
    }
}
