using MediatR;
using NoobGg.Application.Common.Models;

namespace NoobGg.Application.Features.Notifications.Commands.MarkAllRead;

public record MarkAllReadCommand : IRequest<Result>;
