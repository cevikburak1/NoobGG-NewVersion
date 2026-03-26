export interface ConversationResponse {
  id: string;
  partnerId: string;
  partnerUsername: string;
  partnerAvatarUrl: string | null;
  lastMessageContent: string | null;
  lastMessageSenderId: string | null;
  lastMessageAt: string | null;
  unreadCount: number;
}

export interface DirectMessageResponse {
  id: string;
  conversationId: string;
  senderId: string;
  senderUsername: string;
  content: string;
  isRead: boolean;
  createdAt: string;
}

export interface CreateConversationRequest {
  participantId: string;
}

export interface SendDirectMessageRequest {
  content: string;
}
