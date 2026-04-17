using MediatR;
using MongoDB.Driver;
using NoobGg.Application.Common.Constants;
using NoobGg.Application.Common.Interfaces;
using NoobGg.Application.Common.Models;
using NoobGg.Application.Features.GuildEvents.DTOs;
using NoobGg.Domain.Entities;
using NoobGg.Domain.Enums;

namespace NoobGg.Application.Features.GuildEvents.Commands.CreateEvent;

public class CreateGuildEventCommandHandler : IRequestHandler<CreateGuildEventCommand, Result<GuildEventResponse>>
{
    private readonly IMongoContext _mongoContext;
    private readonly ICurrentUser _currentUser;

    public CreateGuildEventCommandHandler(IMongoContext mongoContext, ICurrentUser currentUser)
    {
        _mongoContext = mongoContext;
        _currentUser = currentUser;
    }

    public async Task<Result<GuildEventResponse>> Handle(CreateGuildEventCommand request, CancellationToken ct)
    {
        if (!_currentUser.IsAuthenticated || _currentUser.UserId is null)
            return Result<GuildEventResponse>.Unauthorized();

        var userId = _currentUser.UserId;

        var members = _mongoContext.GetCollection<GuildMember>(CollectionNames.GuildMembers);
        var membership = await members
            .Find(m => m.GuildId == request.GuildId && m.UserId == userId)
            .FirstOrDefaultAsync(ct);

        if (membership is null)
            return Result<GuildEventResponse>.Forbidden("You are not a member of this guild");

        if (membership.Role != GuildMemberRole.Owner && membership.Role != GuildMemberRole.Admin)
            return Result<GuildEventResponse>.Forbidden("Only Admins and Owners can create events");

        var guildEvent = new GuildEvent
        {
            GuildId = request.GuildId,
            CreatorId = userId,
            Title = request.Title,
            Description = request.Description,
            StartsAt = request.StartsAt,
            EndsAt = request.EndsAt,
            GameId = request.GameId,
            TournamentId = request.TournamentId,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        var events = _mongoContext.GetCollection<GuildEvent>(CollectionNames.GuildEvents);
        await events.InsertOneAsync(guildEvent, cancellationToken: ct);

        var username = _currentUser.Username ?? "Unknown";

        var response = new GuildEventResponse(
            guildEvent.Id, guildEvent.GuildId, guildEvent.CreatorId, username,
            guildEvent.Title, guildEvent.Description,
            guildEvent.StartsAt, guildEvent.EndsAt,
            guildEvent.GameId, guildEvent.TournamentId,
            guildEvent.CreatedAt);

        return Result<GuildEventResponse>.Created(response);
    }
}
