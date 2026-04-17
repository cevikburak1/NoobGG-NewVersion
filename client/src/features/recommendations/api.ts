import { api } from '@/lib/api';
import type {
  RecommendedPlayerResponse,
  RecommendedRoomResponse,
  AiRecommendedPlayersResponse,
  RecentPlayerResponse,
  RecentRoomResponse,
} from './types';

export async function getRecommendedPlayers(limit = 6): Promise<RecommendedPlayerResponse[]> {
  const { data } = await api.get<RecommendedPlayerResponse[]>('/api/recommendations/players', {
    params: { limit },
  });
  return data;
}

export async function getAiRecommendedPlayers(limit = 10): Promise<AiRecommendedPlayersResponse> {
  const { data } = await api.get<AiRecommendedPlayersResponse>('/api/recommendations/players/ai', {
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

export async function getRecentPlayers(limit = 5): Promise<RecentPlayerResponse[]> {
  const { data } = await api.get<RecentPlayerResponse[]>('/api/recent/players', {
    params: { limit },
  });
  return data;
}

export async function getRecentRooms(limit = 5): Promise<RecentRoomResponse[]> {
  const { data } = await api.get<RecentRoomResponse[]>('/api/recent/rooms', {
    params: { limit },
  });
  return data;
}
