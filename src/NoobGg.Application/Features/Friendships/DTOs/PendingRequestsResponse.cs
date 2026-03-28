namespace NoobGg.Application.Features.Friendships.DTOs;

public record FriendRequestResponse
{
    public string FriendshipId { get; init; } = string.Empty;
    public string UserId { get; init; } = string.Empty;
    public string Username { get; init; } = string.Empty;
    public string? AvatarUrl { get; init; }
    public DateTime CreatedAt { get; init; }
}

public record PendingRequestsResponse
{
    public List<FriendRequestResponse> Incoming { get; init; } = [];
    public List<FriendRequestResponse> Outgoing { get; init; } = [];
}
