import { api } from '@/lib/api';
import type {
  TournamentListResponse,
  TournamentDetailResponse,
  CreateTournamentPayload,
} from './types';

export async function getTournaments(params: {
  gameId?: string;
  guildId?: string;
  status?: string;
  page?: number;
  pageSize?: number;
}): Promise<TournamentListResponse> {
  const { data } = await api.get<TournamentListResponse>('/api/tournaments', { params });
  return data;
}

export async function getTournamentDetail(id: string): Promise<TournamentDetailResponse> {
  const { data } = await api.get<TournamentDetailResponse>(`/api/tournaments/${id}`);
  return data;
}

export async function createTournament(
  payload: CreateTournamentPayload,
): Promise<TournamentDetailResponse> {
  const { data } = await api.post<TournamentDetailResponse>('/api/tournaments', payload);
  return data;
}

export async function joinTournament(tournamentId: string): Promise<void> {
  await api.post(`/api/tournaments/${tournamentId}/join`);
}

export async function leaveTournament(tournamentId: string): Promise<void> {
  await api.post(`/api/tournaments/${tournamentId}/leave`);
}

export async function generateBracket(tournamentId: string): Promise<void> {
  await api.post(`/api/tournaments/${tournamentId}/generate-bracket`);
}

export async function reportMatchResult(matchId: string, winnerId: string): Promise<void> {
  await api.post(`/api/tournaments/matches/${matchId}/result`, { winnerId });
}
