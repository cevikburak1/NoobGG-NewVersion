export interface RoomResponse {
  id: string;
  title: string;
  gameId: string;
  gameName: string | null;
  gameImageUrl: string | null;
  creatorId: string;
  isPublic: boolean;
  maxMembers: number;
  currentMemberCount: number;
  region: string;
  language: string;
  tags: string[];
  status: string;
  createdAt: string;
  averageElo: number | null;
  averageRankTier: string | null;
}

export interface RoomMemberResponse {
  userId: string;
  username: string;
  avatarUrl: string | null;
  role: string;
  joinedAt: string;
  eloPoints: number | null;
  rankTier: string | null;
}

export interface RoomDetailResponse extends RoomResponse {
  description: string | null;
  gameName: string | null;
  gameImageUrl: string | null;
  rankRange: { min: string; max: string } | null;
  voiceChannelId: string | null;
  members: RoomMemberResponse[];
  averageElo: number | null;
  averageRankTier: string | null;
}

export interface CreateRoomRequest {
  title: string;
  description?: string;
  gameId: string;
  isPublic?: boolean;
  region: string;
  language: string;
  maxMembers?: number;
  tags?: string[];
}

export interface RoomInviteResponse {
  id: string;
  roomId: string;
  roomTitle: string;
  gameName: string | null;
  gameImageUrl: string | null;
  inviterId: string;
  inviterUsername: string;
  inviterAvatarUrl: string | null;
  status: string;
  createdAt: string;
}
