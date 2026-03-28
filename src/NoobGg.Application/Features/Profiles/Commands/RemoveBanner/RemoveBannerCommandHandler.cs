using MediatR;
using MongoDB.Driver;
using NoobGg.Application.Common.Constants;
using NoobGg.Application.Common.Interfaces;
using NoobGg.Application.Common.Models;
using NoobGg.Application.Features.Profiles.DTOs;
using NoobGg.Application.Features.Profiles.Queries.GetProfile;
using NoobGg.Domain.Entities;

namespace NoobGg.Application.Features.Profiles.Commands.RemoveBanner;

public class RemoveBannerCommandHandler : IRequestHandler<RemoveBannerCommand, Result<ProfileDetailResponse>>
{
    private readonly IMongoContext _mongoContext;
    private readonly ICurrentUser _currentUser;
    private readonly IFileStorageService _fileStorage;
    private readonly IMediator _mediator;

    public RemoveBannerCommandHandler(
        IMongoContext mongoContext,
        ICurrentUser currentUser,
        IFileStorageService fileStorage,
        IMediator mediator)
    {
        _mongoContext = mongoContext;
        _currentUser = currentUser;
        _fileStorage = fileStorage;
        _mediator = mediator;
    }

    public async Task<Result<ProfileDetailResponse>> Handle(RemoveBannerCommand request, CancellationToken ct)
    {
        if (!_currentUser.IsAuthenticated || _currentUser.UserId is null)
            return Result<ProfileDetailResponse>.Unauthorized();

        var userId = _currentUser.UserId;
        var profiles = _mongoContext.GetCollection<UserProfile>(CollectionNames.UserProfiles);
        var profile = await profiles.Find(p => p.UserId == userId).FirstOrDefaultAsync(ct);

        if (profile is null)
            return Result<ProfileDetailResponse>.NotFound("Profile not found.");

        var oldBannerUrl = profile.BannerUrl;

        var update = Builders<UserProfile>.Update
            .Set(p => p.BannerUrl, (string?)null)
            .Set(p => p.UpdatedAt, DateTime.UtcNow);

        await profiles.UpdateOneAsync(p => p.Id == profile.Id, update, cancellationToken: ct);

        if (!string.IsNullOrWhiteSpace(oldBannerUrl) && oldBannerUrl.StartsWith("/uploads/"))
            _fileStorage.DeleteFile(oldBannerUrl);

        return await _mediator.Send(new GetProfileQuery { UserId = userId }, ct);
    }
}
