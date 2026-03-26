import { useQuery } from '@tanstack/react-query';
import { queryKeys } from '@/lib/queryKeys';
import { discoverPlayers } from './api';
import type { PlayerDiscoverParams } from './types';

export function useDiscoverPlayers(params: PlayerDiscoverParams) {
  return useQuery({
    queryKey: queryKeys.users.discover(params),
    queryFn: () => discoverPlayers(params),
  });
}
