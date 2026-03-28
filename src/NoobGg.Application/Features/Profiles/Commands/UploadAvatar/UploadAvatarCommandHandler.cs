using MediatR;
using MongoDB.Driver;
using NoobGg.Application.Common.Constants;
using NoobGg.Application.Common.Helpers;
using NoobGg.Application.Common.Interfaces;
using NoobGg.Application.Common.Models;
using NoobGg.Application.Features.Profiles.DTOs;
using NoobGg.Application.Features.Profiles.Queries.GetProfile;
using NoobGg.Domain.Entities;

namespace NoobGg.Application.Features.Profiles.Commands.UploadAvatar;

public class UploadAvatarCommandHandler : IRequestHandler<UploadAvatarCommand, Result<ProfileDetailResponse>>
{
    private const long MaxAvatarSize = 2 * 1024 * 1024; // 2 MB
    private const string Subfolder = "avatars";

    private readonly IMongoContext _mongoContext;
    private readonly ICurrentUser _currentUser;
    private readonly IFileStorageService _fileStorage;
    private readonly IMediator _mediator;

    public UploadAvatarCommandHandler(
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

    public async Task<Result<ProfileDetailResponse>> Handle(UploadAvatarCommand request, CancellationToken ct)
    {
        if (!_currentUser.IsAuthenticated || _currentUser.UserId is null)
            return Result<ProfileDetailResponse>.Unauthorized();

        if (request.FileSize > MaxAvatarSize)
            return Result<ProfileDetailResponse>.Fail("Avatar must be 2 MB or smaller.");

        if (!ImageValidator.AllowedContentTypes.Contains(request.ContentType))
            return Result<ProfileDetailResponse>.Fail("Only JPEG, PNG, and WebP images are allowed.");

        if (!ImageValidator.IsValidImageStream(request.FileStream))
            return Result<ProfileDetailResponse>.Fail("File content does not match a valid image format.");

        var userId = _currentUser.UserId;
        var profiles = _mongoContext.GetCollection<UserProfile>(CollectionNames.UserProfiles);
        var profile = await profiles.Find(p => p.UserId == userId).FirstOrDefaultAsync(ct);

        var oldAvatarUrl = profile?.AvatarUrl;

        var ext = ImageValidator.GetExtensionFromStream(request.FileStream);
        request.FileStream.Position = 0;
        var fileName = $"{userId}_{Guid.NewGuid():N}{ext}";
        var relativeUrl = await _fileStorage.SaveFileAsync(request.FileStream, fileName, Subfolder, ct);

        if (profile is null)
        {
            profile = new UserProfile
            {
                UserId = userId,
                DisplayName = string.Empty,
                AvatarUrl = relativeUrl
            };
            await profiles.InsertOneAsync(profile, cancellationToken: ct);
        }
        else
        {
            var update = Builders<UserProfile>.Update
                .Set(p => p.AvatarUrl, relativeUrl)
                .Set(p => p.UpdatedAt, DateTime.UtcNow);

            await profiles.UpdateOneAsync(p => p.Id == profile.Id, update, cancellationToken: ct);
        }

        if (!string.IsNullOrWhiteSpace(oldAvatarUrl) && oldAvatarUrl.StartsWith("/uploads/"))
            _fileStorage.DeleteFile(oldAvatarUrl);

        return await _mediator.Send(new GetProfileQuery { UserId = userId }, ct);
    }
}
