using MediatR;
using NoobGg.Application.Common.Models;

namespace NoobGg.Application.Features.Notifications.Queries.GetUnreadCount;

public record GetUnreadCountQuery : IRequest<Result<int>>;
