import { api } from '@/lib/api';
import type { RecommendedPlayerResponse, RecommendedRoomResponse } from './types';

export async function getRecommendedPlayers(
  gameId?: string,
  limit: number = 10,
): Promise<RecommendedPlayerResponse[]> {
  const { data } = await api.get<RecommendedPlayerResponse[]>('/api/recommendations/players', {
    params: { gameId, limit },
  });
  return data;
}

export async function getRecommendedRooms(
  gameId?: string,
  limit: number = 10,
): Promise<RecommendedRoomResponse[]> {
  const { data } = await api.get<RecommendedRoomResponse[]>('/api/recommendations/rooms', {
    params: { gameId, limit },
  });
  return data;
}
