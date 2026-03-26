import { api } from '@/lib/api';
import type { PagedResult } from '@/types/api';
import type { DiscoverPlayerResponse, PlayerDiscoverParams } from './types';

export async function discoverPlayers(params: PlayerDiscoverParams): Promise<PagedResult<DiscoverPlayerResponse>> {
  const { data } = await api.get<PagedResult<DiscoverPlayerResponse>>('/api/users/discover', { params });
  return data;
}
