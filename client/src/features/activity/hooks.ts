import { useQuery } from '@tanstack/react-query';
import { queryKeys } from '@/lib/queryKeys';
import { useAuthStore } from '@/stores/authStore';
import { getRecentActivity } from './api';

export function useRecentActivity() {
  const authed = useAuthStore((s) => s.isAuthenticated());

  return useQuery({
    queryKey: queryKeys.users.recentActivity(),
    queryFn: getRecentActivity,
    enabled: authed,
    staleTime: 45_000,
  });
}
