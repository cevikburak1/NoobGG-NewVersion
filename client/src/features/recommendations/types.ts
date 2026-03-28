import type { PlayerGameInfo } from '@/features/users/types';

export interface RecommendedPlayerResponse {
  id: string;
  username: string;
  avatarUrl: string | null;
  bio: string | null;
  country: string | null;
  games: PlayerGameInfo[];
  lookingForTeam: boolean;
  region: string | null;
  experienceLevel: string | null;
  communicationPreference: string | null;
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
