namespace NoobGg.Domain.Entities;

public class Conversation : BaseEntity
{
    public string Participant1Id { get; set; } = string.Empty;
    public string Participant2Id { get; set; } = string.Empty;
    public string? LastMessageContent { get; set; }
    public string? LastMessageSenderId { get; set; }
    public DateTime? LastMessageAt { get; set; }
    public int Participant1UnreadCount { get; set; }
    public int Participant2UnreadCount { get; set; }
}
