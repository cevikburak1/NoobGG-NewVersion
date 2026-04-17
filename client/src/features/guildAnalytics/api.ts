import { api } from '@/lib/api';
import type { GuildStatsResponse } from './types';

export async function getGuildStats(
  guildId: string,
  gameId?: string,
  days = 30,
): Promise<GuildStatsResponse> {
  const { data } = await api.get<GuildStatsResponse>(`/api/guild-analytics/${guildId}`, {
    params: { gameId, days },
  });
  return data;
}
