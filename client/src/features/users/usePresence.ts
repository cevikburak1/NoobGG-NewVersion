import { useQuery } from '@tanstack/react-query';
import { api } from '@/lib/api';

export function usePresence(userId: string | null | undefined) {
  return useQuery({
    queryKey: ['presence', userId],
    queryFn: async () => {
      const { data } = await api.get<{ isOnline: boolean }>(`/api/users/${userId}/presence`);
      return data;
    },
    enabled: Boolean(userId),
    refetchInterval: 15000,
    staleTime: 10000,
  });
}
