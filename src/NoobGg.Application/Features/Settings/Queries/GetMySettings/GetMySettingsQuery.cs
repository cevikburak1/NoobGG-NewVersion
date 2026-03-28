using MediatR;
using NoobGg.Application.Common.Models;
using NoobGg.Application.Features.Settings.DTOs;

namespace NoobGg.Application.Features.Settings.Queries.GetMySettings;

public record GetMySettingsQuery : IRequest<Result<UserSettingsResponse>>;
