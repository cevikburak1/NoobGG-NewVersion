import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { queryKeys } from '@/lib/queryKeys';
import { blockUser, getBlockedUsers, unblockUser } from './api';

export function useBlockedUsers() {
  return useQuery({
    queryKey: queryKeys.blocks.list(),
    queryFn: getBlockedUsers,
  });
}

export function useBlockUser() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: blockUser,
    onSuccess: async () => {
      await queryClient.invalidateQueries({ queryKey: queryKeys.blocks.list() });
    },
  });
}

export function useUnblockUser() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: unblockUser,
    onSuccess: async () => {
      await queryClient.invalidateQueries({ queryKey: queryKeys.blocks.list() });
    },
  });
}
