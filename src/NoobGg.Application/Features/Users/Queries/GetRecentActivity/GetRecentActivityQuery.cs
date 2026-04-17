using MediatR;
using NoobGg.Application.Common.Models;
using NoobGg.Application.Features.Users.DTOs;

namespace NoobGg.Application.Features.Users.Queries.GetRecentActivity;

public record GetRecentActivityQuery : IRequest<Result<RecentActivityResponse>>;
