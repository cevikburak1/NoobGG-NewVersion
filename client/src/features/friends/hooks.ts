import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { queryKeys } from '@/lib/queryKeys';
import {
  getFriends,
  getPendingRequests,
  sendFriendRequest,
  acceptFriendRequest,
  rejectFriendRequest,
  removeFriend,
} from './api';

function invalidateFriendCaches(qc: ReturnType<typeof useQueryClient>) {
  return Promise.all([
    qc.invalidateQueries({ queryKey: queryKeys.friends.list() }),
    qc.invalidateQueries({ queryKey: queryKeys.friends.requests() }),
    qc.invalidateQueries({ queryKey: ['profile'] }),
    qc.invalidateQueries({ queryKey: ['users'] }),
  ]);
}

export function useFriends() {
  return useQuery({
    queryKey: queryKeys.friends.list(),
    queryFn: getFriends,
  });
}

export function usePendingRequests() {
  return useQuery({
    queryKey: queryKeys.friends.requests(),
    queryFn: getPendingRequests,
  });
}

export function useSendFriendRequest() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: sendFriendRequest,
    onSuccess: () => invalidateFriendCaches(qc),
  });
}

export function useAcceptFriendRequest() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: acceptFriendRequest,
    onSuccess: () => invalidateFriendCaches(qc),
  });
}

export function useRejectFriendRequest() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: rejectFriendRequest,
    onSuccess: () => invalidateFriendCaches(qc),
  });
}

export function useRemoveFriend() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: removeFriend,
    onSuccess: () => invalidateFriendCaches(qc),
  });
}
