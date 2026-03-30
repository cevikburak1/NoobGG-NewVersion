import { api } from '@/lib/api';
import type { RecommendedPlayerResponse, RecommendedRoomResponse } from './types';

export async function getRecommendedPlayers(limit = 6): Promise<RecommendedPlayerResponse[]> {
  const { data } = await api.get<RecommendedPlayerResponse[]>('/api/recommendations/players', {
    params: { limit },
  });
  return data;
}

export async function getRecommendedRooms(limit = 6): Promise<RecommendedRoomResponse[]> {
  const { data } = await api.get<RecommendedRoomResponse[]>('/api/recommendations/rooms', {
    params: { limit },
  });
  return data;
}
