import { api } from '@/lib/api';
import type { PagedResult } from '@/types/api';
import type { GameResponse, GameSearchParams, GameBrowseParams } from './types';

export async function searchGames(params: GameSearchParams): Promise<GameResponse[]> {
  const { data } = await api.get<GameResponse[]>('/api/games/search', { params });
  return data;
}

export async function browseGames(params: GameBrowseParams): Promise<PagedResult<GameResponse>> {
  const { data } = await api.get<PagedResult<GameResponse>>('/api/games', { params });
  return data;
}

export async function getGameDetail(gameId: string): Promise<GameResponse> {
  const { data } = await api.get<GameResponse>(`/api/games/${gameId}`);
  return data;
}
