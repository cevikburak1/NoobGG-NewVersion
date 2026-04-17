export interface GuildTopPlayerResponse {
  userId: string;
  username: string;
  avatarUrl: string | null;
  eloPoints: number;
  rankTier: string;
  totalMatches: number;
  wins: number;
  winRate: number;
}

export interface GuildActivityPoint {
  date: string;
  matchesPlayed: number;
  membersJoined: number;
}

export interface GuildStatsResponse {
  guildId: string;
  guildName: string;
  totalMembers: number;
  totalMatches: number;
  totalWins: number;
  overallWinRate: number;
  topPlayers: GuildTopPlayerResponse[];
  activityTimeline: GuildActivityPoint[];
}
