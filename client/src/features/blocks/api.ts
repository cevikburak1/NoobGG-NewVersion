import { api } from '@/lib/api';
import type { BlockedUserResponse } from './types';

export async function getBlockedUsers(): Promise<BlockedUserResponse[]> {
  const { data } = await api.get<BlockedUserResponse[]>('/api/blocks');
  return data;
}

export async function blockUser(userId: string): Promise<void> {
  await api.post(`/api/blocks/${userId}`);
}

export async function unblockUser(userId: string): Promise<void> {
  await api.delete(`/api/blocks/${userId}`);
}
