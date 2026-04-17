using MediatR;
using MongoDB.Driver;
using NoobGg.Application.Common.Constants;
using NoobGg.Application.Common.Interfaces;
using NoobGg.Application.Common.Models;
using NoobGg.Domain.Entities;
using NoobGg.Domain.Enums;

namespace NoobGg.Application.Features.GuildEvents.Commands.DeleteEvent;

public class DeleteGuildEventCommandHandler : IRequestHandler<DeleteGuildEventCommand, Result>
{
    private readonly IMongoContext _mongoContext;
    private readonly ICurrentUser _currentUser;

    public DeleteGuildEventCommandHandler(IMongoContext mongoContext, ICurrentUser currentUser)
    {
        _mongoContext = mongoContext;
        _currentUser = currentUser;
    }

    public async Task<Result> Handle(DeleteGuildEventCommand request, CancellationToken ct)
    {
        if (!_currentUser.IsAuthenticated || _currentUser.UserId is null)
            return Result.Fail("Unauthorized", 401);

        var userId = _currentUser.UserId;

        var events = _mongoContext.GetCollection<GuildEvent>(CollectionNames.GuildEvents);
        var guildEvent = await events.Find(e => e.Id == request.EventId).FirstOrDefaultAsync(ct);

        if (guildEvent is null)
            return Result.Fail("Event not found", 404);

        var members = _mongoContext.GetCollection<GuildMember>(CollectionNames.GuildMembers);
        var membership = await members
            .Find(m => m.GuildId == guildEvent.GuildId && m.UserId == userId)
            .FirstOrDefaultAsync(ct);

        if (membership is null)
            return Result.Fail("Forbidden", 403);

        if (membership.Role != GuildMemberRole.Owner && membership.Role != GuildMemberRole.Admin)
            return Result.Fail("Only Admins and Owners can delete events", 403);

        await events.DeleteOneAsync(e => e.Id == request.EventId, ct);

        return Result.Success();
    }
}
