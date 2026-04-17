import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { queryKeys } from '@/lib/queryKeys';
import {
  getTournaments,
  getTournamentDetail,
  createTournament,
  joinTournament,
  leaveTournament,
  generateBracket,
  reportMatchResult,
} from './api';
import type { CreateTournamentPayload } from './types';

export function useTournaments(params: {
  gameId?: string;
  guildId?: string;
  status?: string;
  page?: number;
}) {
  return useQuery({
    queryKey: queryKeys.tournaments.list(params),
    queryFn: () => getTournaments(params),
  });
}

export function useTournamentDetail(id: string | undefined) {
  return useQuery({
    queryKey: queryKeys.tournaments.detail(id ?? ''),
    queryFn: () => getTournamentDetail(id!),
    enabled: Boolean(id),
    refetchInterval: 10_000,
  });
}

export function useCreateTournament() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (payload: CreateTournamentPayload) => createTournament(payload),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ['tournaments'] });
    },
  });
}

export function useJoinTournament() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (tournamentId: string) => joinTournament(tournamentId),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ['tournaments'] });
    },
  });
}

export function useLeaveTournament() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (tournamentId: string) => leaveTournament(tournamentId),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ['tournaments'] });
    },
  });
}

export function useGenerateBracket() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (tournamentId: string) => generateBracket(tournamentId),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ['tournaments'] });
    },
  });
}

export function useReportMatchResult() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: ({ matchId, winnerId }: { matchId: string; winnerId: string }) =>
      reportMatchResult(matchId, winnerId),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ['tournaments'] });
    },
  });
}
