import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { queryKeys } from '@/lib/queryKeys';
import { blockUser, getBlockedUsers, unblockUser } from './api';

export function useBlockedUsers() {
  return useQuery({
    queryKey: queryKeys.blocks.list(),
    queryFn: getBlockedUsers,
  });
}

async function invalidateBlockRelatedCaches(qc: ReturnType<typeof useQueryClient>) {
  await Promise.all([
    qc.invalidateQueries({ queryKey: queryKeys.blocks.list() }),
    qc.invalidateQueries({ queryKey: queryKeys.dm.conversations() }),
    qc.invalidateQueries({ queryKey: ['users'] }),
    qc.invalidateQueries({ queryKey: ['profile'] }),
    qc.invalidateQueries({ queryKey: queryKeys.rooms.all() }),
  ]);
}

export function useBlockUser() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: blockUser,
    onSuccess: () => invalidateBlockRelatedCaches(queryClient),
  });
}

export function useUnblockUser() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: unblockUser,
    onSuccess: () => invalidateBlockRelatedCaches(queryClient),
  });
}
