using NoobGg.Application.Features.Chat.DTOs;

namespace NoobGg.Api.Hubs.Contracts;

/// <summary>
/// Strongly-typed client-side events for room chat.
/// Method names here become the event names on the frontend.
/// </summary>
public interface IChatClient
{
    Task ReceiveMessage(ChatMessageResponse message);
    Task UserJoined(ChatPresenceEvent presence);
    Task UserLeft(ChatPresenceEvent presence);
    Task UserStartedTyping(TypingEvent typing);
    Task UserStoppedTyping(TypingEvent typing);
    Task RoomPresenceUpdated(RoomPresenceResponse presence);
}
