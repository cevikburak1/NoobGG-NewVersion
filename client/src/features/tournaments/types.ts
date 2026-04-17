export interface TournamentListItemResponse {
  id: string;
  name: string;
  description: string | null;
  gameId: string;
  gameName: string;
  organizerId: string;
  organizerUsername: string;
  guildId: string | null;
  format: string;
  status: string;
  maxParticipants: number;
  currentParticipants: number;
  registrationDeadline: string;
  startsAt: string | null;
  prizeBadges: string[];
  createdAt: string;
}

export interface TournamentEntryResponse {
  id: string;
  participantId: string;
  participantName: string;
  entryType: string;
  guildId: string | null;
  seed: number;
  isEliminated: boolean;
  placement: number;
  earnedBadges: string[];
}

export interface TournamentMatchResponse {
  id: string;
  round: number;
  matchNumber: number;
  participant1Id: string | null;
  participant1Name: string | null;
  participant2Id: string | null;
  participant2Name: string | null;
  winnerId: string | null;
  status: string;
  nextMatchId: string | null;
}

export interface TournamentDetailResponse {
  id: string;
  name: string;
  description: string | null;
  gameId: string;
  gameName: string;
  organizerId: string;
  organizerUsername: string;
  guildId: string | null;
  format: string;
  status: string;
  maxParticipants: number;
  currentParticipants: number;
  registrationDeadline: string;
  startsAt: string | null;
  currentRound: number;
  totalRounds: number;
  prizeBadges: string[];
  entries: TournamentEntryResponse[];
  matches: TournamentMatchResponse[];
  isParticipant: boolean;
  createdAt: string;
}

export interface TournamentListResponse {
  tournaments: TournamentListItemResponse[];
  totalCount: number;
  hasMore: boolean;
}

export interface CreateTournamentPayload {
  name: string;
  description?: string;
  gameId: string;
  guildId?: string;
  format: number;
  maxParticipants: number;
  registrationDeadline: string;
  startsAt?: string;
  prizeBadges?: string[];
}
