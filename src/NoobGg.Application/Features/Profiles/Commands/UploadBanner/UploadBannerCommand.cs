using MediatR;
using NoobGg.Application.Common.Models;
using NoobGg.Application.Features.Profiles.DTOs;

namespace NoobGg.Application.Features.Profiles.Commands.UploadBanner;

public record UploadBannerCommand : IRequest<Result<ProfileDetailResponse>>
{
    public required Stream FileStream { get; init; }
    public required string ContentType { get; init; }
    public required long FileSize { get; init; }
}
