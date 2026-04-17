import { api } from '@/lib/api';
import type { GuildEventListResponse, GuildEventResponse, CreateGuildEventPayload } from './types';

export async function getGuildEvents(
  guildId: string,
  from?: string,
  to?: string,
): Promise<GuildEventListResponse> {
  const { data } = await api.get<GuildEventListResponse>(`/api/guild-events/${guildId}`, {
    params: { from, to },
  });
  return data;
}

export async function createGuildEvent(
  payload: CreateGuildEventPayload,
): Promise<GuildEventResponse> {
  const { data } = await api.post<GuildEventResponse>('/api/guild-events', payload);
  return data;
}

export async function deleteGuildEvent(eventId: string): Promise<void> {
  await api.delete(`/api/guild-events/${eventId}`);
}
