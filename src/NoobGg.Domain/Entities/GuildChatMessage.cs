namespace NoobGg.Domain.Entities;

public class GuildChatMessage : BaseEntity
{
    public string GuildId { get; set; } = string.Empty;
    public string SenderId { get; set; } = string.Empty;
    public string SenderUsername { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
}
