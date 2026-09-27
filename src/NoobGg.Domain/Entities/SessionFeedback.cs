namespace NoobGg.Domain.Entities;

public class SessionFeedback : BaseEntity
{
    public string SessionId { get; set; } = string.Empty;
    public string UserId { get; set; } = string.Empty;
    public bool WouldPlayAgain { get; set; }
    public List<string> Tags { get; set; } = [];
}
