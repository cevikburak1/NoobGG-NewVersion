import { api } from '@/lib/api';
import type { FavoritePlayerResponse } from './types';

export async function getMyFavorites(): Promise<FavoritePlayerResponse[]> {
  const { data } = await api.get<FavoritePlayerResponse[]>('/api/favorites');
  return data;
}

export async function addFavorite(userId: string): Promise<void> {
  await api.post(`/api/favorites/${userId}`);
}

export async function removeFavorite(userId: string): Promise<void> {
  await api.delete(`/api/favorites/${userId}`);
}
