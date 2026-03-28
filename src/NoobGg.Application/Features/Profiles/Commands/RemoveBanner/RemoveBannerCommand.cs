using MediatR;
using NoobGg.Application.Common.Models;
using NoobGg.Application.Features.Profiles.DTOs;

namespace NoobGg.Application.Features.Profiles.Commands.RemoveBanner;

public record RemoveBannerCommand : IRequest<Result<ProfileDetailResponse>>;
