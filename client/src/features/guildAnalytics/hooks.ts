import { useQuery } from '@tanstack/react-query';
import { queryKeys } from '@/lib/queryKeys';
import { getGuildStats } from './api';

export function useGuildStats(guildId: string | undefined, gameId?: string, days = 30) {
  return useQuery({
    queryKey: queryKeys.guildAnalytics.stats(guildId ?? '', gameId, days),
    queryFn: () => getGuildStats(guildId!, gameId, days),
    enabled: Boolean(guildId),
  });
}
