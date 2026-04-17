using MediatR;
using NoobGg.Application.Common.Models;

namespace NoobGg.Application.Features.Matchmaking.Commands.LeaveMatchQueue;

public record LeaveMatchQueueCommand : IRequest<Result>;
