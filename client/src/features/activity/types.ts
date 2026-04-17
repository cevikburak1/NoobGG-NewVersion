export type RecentPlayerSource = 'directMessage' | 'friendship' | 'room';

export interface RecentPlayerItem {
  userId: string;
  username: string;
  avatarUrl?: string | null;
  lastInteractionAt: string;
  source: RecentPlayerSource;
}

export interface RecentConversationItem {
  id: string;
  partnerId: string;
  partnerUsername: string;
  partnerAvatarUrl?: string | null;
  lastMessageContent?: string | null;
  lastMessageSenderId?: string | null;
  lastMessageAt?: string | null;
  unreadCount: number;
}

export interface RecentRoomItem {
  roomId: string;
  title: string;
  gameId: string;
  gameName?: string | null;
  gameImageUrl?: string | null;
  joinedAt: string;
  currentMemberCount: number;
  region: string;
  status: string;
}

export interface RecentActivityResponse {
  recentPlayers: RecentPlayerItem[];
  recentConversations: RecentConversationItem[];
  recentRooms: RecentRoomItem[];
}
