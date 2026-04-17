export interface RecommendedPlayerGameInfo {
  gameId: string;
  gameName: string;
  gameImageUrl: string | null;
  rank: string;
  experienceLevel: string;
  lookingForTeam: boolean;
}

export interface RecommendedPlayerResponse {
  id: string;
  username: string;
  avatarUrl: string | null;
  bio: string | null;
  country: string | null;
  games: RecommendedPlayerGameInfo[];
  lookingForTeam: boolean;
  region: string | null;
  experienceLevel: string | null;
  communicationPreference: string | null;
  friendshipStatus: string | null;
  score: number;
  matchReasons: string[];
}

export interface RecommendedRoomResponse {
  id: string;
  title: string;
  gameId: string;
  gameName: string | null;
  gameImageUrl: string | null;
  creatorId: string;
  maxMembers: number;
  currentMemberCount: number;
  region: string;
  language: string;
  tags: string[];
  status: string;
  createdAt: string;
  score: number;
  matchReasons: string[];
}

export interface AiRecommendedPlayersResponse {
  players: RecommendedPlayerResponse[];
  usedAi: boolean;
}

export interface RecentPlayerResponse {
  id: string;
  username: string;
  avatarUrl: string | null;
  country: string | null;
  isOnline: boolean;
  seenAt: string;
}

export interface RecentRoomResponse {
  id: string;
  title: string;
  gameName: string | null;
  gameImageUrl: string | null;
  status: string;
  currentMemberCount: number;
  maxMembers: number;
  seenAt: string;
}
