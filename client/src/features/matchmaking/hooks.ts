import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { queryKeys } from '@/lib/queryKeys';
import { getMatchQueueStatus, joinMatchQueue, leaveMatchQueue } from './api';
import type { JoinMatchQueueRequest } from './types';

export function useMatchQueueStatus(enabled: boolean) {
  return useQuery({
    queryKey: queryKeys.matchmaking.queueStatus(),
    queryFn: getMatchQueueStatus,
    enabled,
    refetchInterval: (query) => {
      if (!enabled) return false;
      const s = query.state.data;
      if (!s) return false;
      if (s.status === 'Searching' || s.status === 'FallbackSuggested') return 2500;
      return false;
    },
  });
}

export function useJoinMatchQueue() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (payload: JoinMatchQueueRequest) => joinMatchQueue(payload),
    onSuccess: (res) => {
      void qc.invalidateQueries({ queryKey: queryKeys.matchmaking.queueStatus() });
      if (res.status === 'Matched') {
        void qc.invalidateQueries({ queryKey: queryKeys.rooms.all() });
        void qc.invalidateQueries({ queryKey: queryKeys.users.recentActivity() });
      }
    },
  });
}

export function useLeaveMatchQueue() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: leaveMatchQueue,
    onSuccess: () => {
      void qc.invalidateQueries({ queryKey: queryKeys.matchmaking.queueStatus() });
    },
  });
}
