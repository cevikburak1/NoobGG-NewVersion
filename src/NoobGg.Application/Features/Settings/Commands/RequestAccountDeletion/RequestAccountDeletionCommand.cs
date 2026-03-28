using MediatR;
using NoobGg.Application.Common.Models;

namespace NoobGg.Application.Features.Settings.Commands.RequestAccountDeletion;

public record RequestAccountDeletionCommand : IRequest<Result>;
