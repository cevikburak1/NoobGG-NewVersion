export interface PlayerGameInfo {
  gameId: string;
  gameName: string;
  gameImageUrl: string | null;
  rank: string;
  experienceLevel: string;
  lookingForTeam: boolean;
}

export interface DiscoverPlayerResponse {
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
}

export interface PlayerDiscoverParams {
  search?: string;
  gameId?: string;
  region?: string;
  experienceLevel?: string;
  lookingForTeam?: boolean;
  page?: number;
  pageSize?: number;
}
