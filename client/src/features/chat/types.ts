export interface ChatMessageResponse {
  id: string;
  roomId: string;
  senderId: string;
  senderUsername: string;
  content: string;
  type: string;
  isEdited: boolean;
  createdAt: string;
  editedAt: string | null;
}

export interface ChatPresenceEvent {
  userId: string;
  username: string;
  roomId: string;
  timestamp: string;
}

export interface TypingEvent {
  userId: string;
  username: string;
  roomId: string;
}

export interface RoomPresenceResponse {
  roomId: string;
  onlineUsers: OnlineUser[];
  onlineCount: number;
}

export interface OnlineUser {
  userId: string;
  username: string;
}

export interface MessageDeletedEvent {
  messageId: string;
  roomId: string;
}

export interface MessageEditedEvent {
  messageId: string;
  roomId: string;
  content: string;
  editedAt: string;
}
