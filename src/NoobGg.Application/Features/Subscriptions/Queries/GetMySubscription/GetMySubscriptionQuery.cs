using MediatR;
using NoobGg.Application.Common.Models;
using NoobGg.Application.Features.Subscriptions.DTOs;

namespace NoobGg.Application.Features.Subscriptions.Queries.GetMySubscription;

public record GetMySubscriptionQuery : IRequest<Result<UserSubscriptionResponse>>;
