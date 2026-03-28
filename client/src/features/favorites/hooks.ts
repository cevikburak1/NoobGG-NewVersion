import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { queryKeys } from '@/lib/queryKeys';
import { addFavorite, getMyFavorites, removeFavorite } from './api';

export function useMyFavorites() {
  return useQuery({
    queryKey: queryKeys.favorites.list(),
    queryFn: getMyFavorites,
  });
}

export function useToggleFavorite(targetUserId: string) {
  const qc = useQueryClient();

  const add = useMutation({
    mutationFn: () => addFavorite(targetUserId),
    onSuccess: () => {
      void qc.invalidateQueries({ queryKey: queryKeys.favorites.list() });
      void qc.invalidateQueries({ queryKey: queryKeys.profile.detail(targetUserId) });
    },
  });

  const remove = useMutation({
    mutationFn: () => removeFavorite(targetUserId),
    onSuccess: () => {
      void qc.invalidateQueries({ queryKey: queryKeys.favorites.list() });
      void qc.invalidateQueries({ queryKey: queryKeys.profile.detail(targetUserId) });
    },
  });

  return { add, remove, isLoading: add.isPending || remove.isPending };
}
