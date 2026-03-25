using NoobGg.Domain.Enums;

namespace NoobGg.Domain.Entities;

public class Notification : BaseEntity
{
    public string UserId { get; set; } = string.Empty;
    public NotificationType Type { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
    public Dictionary<string, string>? Data { get; set; }
    public bool IsRead { get; set; }
    public DateTime? ReadAt { get; set; }
}
