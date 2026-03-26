using MediatR;
using MongoDB.Driver;
using NoobGg.Application.Common.Constants;
using NoobGg.Application.Common.Interfaces;
using NoobGg.Application.Common.Models;
using NoobGg.Application.Features.Profiles.DTOs;
using NoobGg.Application.Features.Profiles.Queries.GetProfile;
using NoobGg.Domain.Entities;
using NoobGg.Domain.ValueObjects;

namespace NoobGg.Application.Features.Profiles.Commands.UpdateProfile;

public class UpdateProfileCommandHandler : IRequestHandler<UpdateProfileCommand, Result<ProfileDetailResponse>>
{
    private readonly IMongoContext _mongoContext;
    private readonly ICurrentUser _currentUser;
    private readonly IMediator _mediator;

    public UpdateProfileCommandHandler(IMongoContext mongoContext, ICurrentUser currentUser, IMediator mediator)
    {
        _mongoContext = mongoContext;
        _currentUser = currentUser;
        _mediator = mediator;
    }

    public async Task<Result<ProfileDetailResponse>> Handle(UpdateProfileCommand request, CancellationToken ct)
    {
        if (!_currentUser.IsAuthenticated || _currentUser.UserId is null)
            return Result<ProfileDetailResponse>.Unauthorized();

        var userId = _currentUser.UserId;
        var profiles = _mongoContext.GetCollection<UserProfile>(CollectionNames.UserProfiles);
        var users = _mongoContext.GetCollection<User>(CollectionNames.Users);

        var profile = await profiles.Find(p => p.UserId == userId).FirstOrDefaultAsync(ct);

        if (profile is null)
        {
            profile = new UserProfile
            {
                UserId = userId,
                DisplayName = request.DisplayName ?? string.Empty,
                AvatarUrl = request.AvatarUrl,
                Bio = request.Bio,
                Country = request.Country,
                Timezone = request.Timezone,
                Availability = BuildAvailability(request)
            };
            await profiles.InsertOneAsync(profile, cancellationToken: ct);
        }
        else
        {
            var update = Builders<UserProfile>.Update
                .Set(p => p.DisplayName, request.DisplayName ?? profile.DisplayName)
                .Set(p => p.AvatarUrl, request.AvatarUrl ?? profile.AvatarUrl)
                .Set(p => p.Bio, request.Bio ?? profile.Bio)
                .Set(p => p.Country, request.Country ?? profile.Country)
                .Set(p => p.Timezone, request.Timezone ?? profile.Timezone)
                .Set(p => p.Availability, BuildAvailability(request) ?? profile.Availability)
                .Set(p => p.UpdatedAt, DateTime.UtcNow);

            await profiles.UpdateOneAsync(p => p.Id == profile.Id, update, cancellationToken: ct);
        }

        await users.UpdateOneAsync(
            u => u.Id == userId,
            Builders<User>.Update
                .Set(u => u.IsProfileComplete, true)
                .Set(u => u.UpdatedAt, DateTime.UtcNow),
            cancellationToken: ct);

        return await _mediator.Send(new GetProfileQuery { UserId = userId }, ct);
    }

    private static Availability? BuildAvailability(UpdateProfileCommand req)
    {
        if (req.WeekdaysFrom is null && req.WeekendsFrom is null)
            return null;

        return new Availability
        {
            Weekdays = req.WeekdaysFrom is not null
                ? new TimeSlot { From = req.WeekdaysFrom, To = req.WeekdaysTo ?? "" }
                : null,
            Weekends = req.WeekendsFrom is not null
                ? new TimeSlot { From = req.WeekendsFrom, To = req.WeekendsTo ?? "" }
                : null
        };
    }
}
