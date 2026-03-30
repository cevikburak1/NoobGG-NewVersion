import { api } from '@/lib/api';
import type { PagedResult, GuildFilters } from '@/types/api';
import type {
  CreateGuildRequest,
  GuildDetailResponse,
  GuildInviteResponse,
  GuildResponse,
} from '@/features/guilds/types';

export async function getGuilds(filters: GuildFilters): Promise<PagedResult<GuildResponse>> {
  const { data } = await api.get<PagedResult<GuildResponse>>('/api/guilds', { params: filters });
  return data;
}

export async function getGuildDetail(id: string): Promise<GuildDetailResponse> {
  const { data } = await api.get<GuildDetailResponse>(`/api/guilds/${id}`);
  return data;
}

export async function createGuild(data: CreateGuildRequest): Promise<GuildDetailResponse> {
  const { data: body } = await api.post<GuildDetailResponse>('/api/guilds', data);
  return body;
}

export async function joinGuild(id: string): Promise<void> {
  await api.post(`/api/guilds/${id}/join`);
}

export async function leaveGuild(id: string): Promise<void> {
  await api.post(`/api/guilds/${id}/leave`);
}

export async function kickGuildMember(guildId: string, userId: string): Promise<void> {
  await api.post(`/api/guilds/${guildId}/kick`, { userId });
}

export async function updateGuildMemberRole(
  guildId: string,
  userId: string,
  newRole: string,
): Promise<void> {
  await api.post(`/api/guilds/${guildId}/role`, { userId, newRole });
}

export async function inviteToGuild(guildId: string, userId: string): Promise<void> {
  await api.post(`/api/guilds/${guildId}/invite/${userId}`);
}

export async function getPendingGuildInvites(): Promise<GuildInviteResponse[]> {
  const { data } = await api.get<GuildInviteResponse[]>('/api/guilds/invites');
  return data;
}

export async function acceptGuildInvite(inviteId: string): Promise<void> {
  await api.post(`/api/guilds/invites/${inviteId}/accept`);
}

export async function declineGuildInvite(inviteId: string): Promise<void> {
  await api.post(`/api/guilds/invites/${inviteId}/decline`);
}
