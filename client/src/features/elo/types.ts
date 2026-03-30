export interface LeaderboardEntry {
  position: number;
  userId: string;
  username: string;
  avatarUrl: string | null;
  eloPoints: number;
  rankTier: string;
  hoursPlayed: number | null;
  lookingForTeam: boolean;
}

export interface EloSnapshot {
  points: number;
  recordedAt: string;
}

export interface RecentMatch {
  matchId: string;
  opponentId: string;
  opponentUsername: string;
  won: boolean;
  eloChange: number;
  eloBefore: number;
  playedAt: string;
}

export interface EloHistoryResponse {
  currentElo: number;
  rankTier: string;
  gameId: string;
  gameName: string;
  history: EloSnapshot[];
  recentMatches: RecentMatch[];
}

export interface RecordMatchRequest {
  gameId: string;
  opponentId: string;
  won: boolean;
}

export interface SubmitSessionResultsRequest {
  roomId: string;
  wins: number;
  losses: number;
}
