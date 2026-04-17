import { api } from '@/lib/api';
import type { GetMatchQueueStatusResponse, JoinMatchQueueResponse, JoinMatchQueueRequest } from './types';

export async function joinMatchQueue(
  payload: JoinMatchQueueRequest,
): Promise<JoinMatchQueueResponse> {
  const { data } = await api.post<JoinMatchQueueResponse>('/api/matchmaking/queue', payload);
  return data;
}

export async function leaveMatchQueue(): Promise<void> {
  await api.delete('/api/matchmaking/queue');
}

export async function getMatchQueueStatus(): Promise<GetMatchQueueStatusResponse> {
  const { data } = await api.get<GetMatchQueueStatusResponse>('/api/matchmaking/queue/status');
  return data;
}
