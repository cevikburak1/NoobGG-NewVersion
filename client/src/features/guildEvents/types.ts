export interface GuildEventResponse {
  id: string;
  guildId: string;
  creatorId: string;
  creatorUsername: string;
  title: string;
  description: string | null;
  startsAt: string;
  endsAt: string;
  gameId: string | null;
  tournamentId: string | null;
  createdAt: string;
}

export interface GuildEventListResponse {
  events: GuildEventResponse[];
  totalCount: number;
}

export interface CreateGuildEventPayload {
  guildId: string;
  title: string;
  description?: string;
  startsAt: string;
  endsAt: string;
  gameId?: string;
  tournamentId?: string;
}
