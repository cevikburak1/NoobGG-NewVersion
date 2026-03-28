using MediatR;
using NoobGg.Application.Common.Models;
using NoobGg.Application.Features.Friendships.DTOs;

namespace NoobGg.Application.Features.Friendships.Queries.GetPendingRequests;

public class GetPendingRequestsQuery : IRequest<Result<PendingRequestsResponse>>
{
}
