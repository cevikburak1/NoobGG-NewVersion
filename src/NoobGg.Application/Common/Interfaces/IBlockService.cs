namespace NoobGg.Application.Common.Interfaces;

/// <summary>
/// Checks block relationships between users.
/// Injected into chat hub, room handlers, etc. to enforce block rules.
/// </summary>
public interface IBlockService
{
    /// <summary>One-way check: did blockerId block blockedUserId?</summary>
    Task<bool> IsBlockedAsync(string blockerId, string blockedUserId, CancellationToken ct = default);

    /// <summary>Bidirectional check: is there a block in either direction?</summary>
    Task<bool> HasBlockBetweenAsync(string userA, string userB, CancellationToken ct = default);

    /// <summary>Returns IDs of all users blocked by this user.</summary>
    Task<HashSet<string>> GetBlockedUserIdsAsync(string userId, CancellationToken ct = default);
}
