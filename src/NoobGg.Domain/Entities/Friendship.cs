using NoobGg.Domain.Enums;

namespace NoobGg.Domain.Entities;

public class Friendship : BaseEntity
{
    public string RequesterId { get; set; } = string.Empty;
    public string AddresseeId { get; set; } = string.Empty;
    public FriendshipStatus Status { get; set; } = FriendshipStatus.Pending;
    public DateTime? RespondedAt { get; set; }
}
