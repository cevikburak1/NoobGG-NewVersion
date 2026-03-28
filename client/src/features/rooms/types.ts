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
}

export interface RoomMemberResponse {
  userId: string;
  username: string;
  avatarUrl: string | null;
  role: string;
  joinedAt: string;
}

export interface RoomDetailResponse extends RoomResponse {
  description: string | null;
  gameName: string | null;
  gameImageUrl: string | null;
  rankRange: { min: string; max: string } | null;
  voiceChannelId: string | null;
  members: RoomMemberResponse[];
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
