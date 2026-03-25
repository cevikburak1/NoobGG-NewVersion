using NoobGg.Domain.Enums;

namespace NoobGg.Domain.Entities;

public class Message : BaseEntity
{
    public string RoomId { get; set; } = string.Empty;
    public string SenderId { get; set; } = string.Empty;
    public string SenderUsername { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public MessageType Type { get; set; } = MessageType.Text;
    public bool IsFiltered { get; set; }
}
