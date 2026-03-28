import { useQuery } from '@tanstack/react-query';
import { queryKeys } from '@/lib/queryKeys';
import { getRecommendedPlayers, getRecommendedRooms } from './api';

export function useRecommendedPlayers(gameId?: string, limit?: number) {
  return useQuery({
    queryKey: queryKeys.recommendations.players(gameId),
    queryFn: () => getRecommendedPlayers(gameId, limit),
    refetchInterval: 60_000,
  });
}

export function useRecommendedRooms(gameId?: string, limit?: number) {
  return useQuery({
    queryKey: queryKeys.recommendations.rooms(gameId),
    queryFn: () => getRecommendedRooms(gameId, limit),
    refetchInterval: 60_000,
  });
}
