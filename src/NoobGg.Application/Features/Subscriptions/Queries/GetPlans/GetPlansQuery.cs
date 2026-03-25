using MediatR;
using NoobGg.Application.Common.Models;
using NoobGg.Application.Features.Subscriptions.DTOs;

namespace NoobGg.Application.Features.Subscriptions.Queries.GetPlans;

public record GetPlansQuery : IRequest<Result<PlanComparisonResponse>>;
