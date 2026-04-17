import { api } from '@/lib/api';
import type { RecentActivityResponse } from './types';

export async function getRecentActivity(): Promise<RecentActivityResponse> {
  const { data } = await api.get<RecentActivityResponse>('/api/users/recent-activity');
  return data;
}
