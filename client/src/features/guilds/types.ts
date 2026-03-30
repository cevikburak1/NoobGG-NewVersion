export interface GuildResponse {
  id: string;
  name: string;
  tag: string;
  description: string | null;
  creatorId: string;
  isPublic: boolean;
  maxMembers: number;
  currentMemberCount: number;
  region: string;
  language: string;
  gameIds: string[];
  gameNames: string[];
  createdAt: string;
}

export interface GuildGameInfo {
  id: string;
  name: string;
  backgroundImageUrl: string | null;
}

export interface GuildMemberResponse {
  userId: string;
  username: string;
  avatarUrl: string | null;
  role: string;
  joinedAt: string;
}

export interface GuildDetailResponse {
  id: string;
  name: string;
  tag: string;
  description: string | null;
  creatorId: string;
  isPublic: boolean;
  maxMembers: number;
  currentMemberCount: number;
  region: string;
  language: string;
  gameIds: string[];
  games: GuildGameInfo[];
  createdAt: string;
  members: GuildMemberResponse[];
}

export interface CreateGuildRequest {
  name: string;
  tag: string;
  description?: string;
  isPublic?: boolean;
  region: string;
  language: string;
  gameIds?: string[];
}

export interface GuildInviteResponse {
  id: string;
  guildId: string;
  guildName: string;
  guildTag: string;
  inviterId: string;
  inviterUsername: string;
  inviterAvatarUrl: string | null;
  status: string;
  createdAt: string;
}
