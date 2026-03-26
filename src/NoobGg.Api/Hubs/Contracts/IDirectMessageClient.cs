using NoobGg.Application.Features.DirectMessages.DTOs;

namespace NoobGg.Api.Hubs.Contracts;

public interface IDirectMessageClient
{
    Task ReceiveDirectMessage(DirectMessageResponse message);
    Task ConversationUpdated(ConversationResponse conversation);
    Task MessagesRead(string conversationId, string readByUserId);
    Task UserTypingDM(string conversationId, string userId, string username);
    Task UserStoppedTypingDM(string conversationId, string userId);
}
