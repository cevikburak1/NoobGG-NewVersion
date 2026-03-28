export interface ProfileDetailResponse {
  userId: string;
  username: string;
  displayName: string | null;
  avatarUrl: string | null;
  bannerUrl: string | null;
  bio: string | null;
  country: string | null;
  timezone: string | null;
  region: string | null;
  language: string | null;
  experienceLevel: string | null;
  communicationPreference: string | null;
  playSchedule: string | null;
  isProfileComplete: boolean;
  createdAt: string;
  games: GameProfileResponse[];
  stats: ProfileStats;
  isOwnProfile: boolean;
  isOnline: boolean;
  isBlocked: boolean;
  isBlockedByThem: boolean;
  isRestricted: boolean;
  restrictedReason: string | null;
  friendshipStatus: string | null;
  friendshipId: string | null;
  isFriendRequestSentByMe: boolean;
}

export interface ProfileStats {
  roomsJoined: number;
  roomsCreated: number;
  gamesPlayed: number;
}

export interface GameProfileResponse {
  id: string;
  userId: string;
  gameId: string;
  gameName: string;
  gameImageUrl: string | null;
  rank: string;
  role: string | null;
  region: string;
  experienceLevel: string;
  communicationPreference: string;
  hoursPlayed: number | null;
  lookingForTeam: boolean;
  note: string | null;
  inGameName: string | null;
}

export interface UpdateProfileRequest {
  displayName?: string;
  avatarUrl?: string;
  bio?: string;
  country?: string;
  timezone?: string;
  weekdaysFrom?: string;
  weekdaysTo?: string;
  weekendsFrom?: string;
  weekendsTo?: string;
}

export interface AddGameProfileRequest {
  gameId: string;
  rank: string;
  role?: string;
  region: string;
  experienceLevel: string;
  communicationPreference: string;
  hoursPlayed?: number;
  lookingForTeam: boolean;
  note?: string;
  inGameName?: string;
}

export interface UpdateGameProfileRequest {
  rank?: string;
  role?: string;
  region?: string;
  experienceLevel?: string;
  communicationPreference?: string;
  hoursPlayed?: number;
  lookingForTeam?: boolean;
  note?: string;
  inGameName?: string;
}
