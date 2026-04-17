using NoobGg.Domain.Enums;

namespace NoobGg.Application.Common.Interfaces;

public interface IRecentActivityService
{
    Task UpsertAsync(string userId, string targetId, RecentActivityTargetType targetType, CancellationToken ct = default);
}
