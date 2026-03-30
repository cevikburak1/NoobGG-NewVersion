import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { queryKeys } from '@/lib/queryKeys';
import { getLeaderboard, getEloHistory, recordMatch, submitSessionResults } from './api';
import type { RecordMatchRequest, SubmitSessionResultsRequest } from './types';

export function useLeaderboard(gameId: string, page = 1, pageSize = 50) {
  return useQuery({
    queryKey: queryKeys.elo.leaderboard(gameId, page),
    queryFn: () => getLeaderboard(gameId, page, pageSize),
    enabled: !!gameId,
  });
}

export function useEloHistory(userId: string, gameId: string) {
  return useQuery({
    queryKey: queryKeys.elo.history(userId, gameId),
    queryFn: () => getEloHistory(userId, gameId),
    enabled: !!userId && !!gameId,
  });
}

export function useRecordMatch() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (request: RecordMatchRequest) => recordMatch(request),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['elo'] });
      queryClient.invalidateQueries({ queryKey: ['profile'] });
    },
  });
}

export function useSubmitSessionResults() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (request: SubmitSessionResultsRequest) => submitSessionResults(request),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['elo'] });
      queryClient.invalidateQueries({ queryKey: ['profile'] });
    },
  });
}
