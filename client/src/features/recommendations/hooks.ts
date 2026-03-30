import { useQuery } from '@tanstack/react-query';
import { queryKeys } from '@/lib/queryKeys';
import { getRecommendedPlayers, getRecommendedRooms } from './api';

export function useRecommendedPlayers(limit = 6) {
  return useQuery({
    queryKey: queryKeys.recommendations.players(limit),
    queryFn: () => getRecommendedPlayers(limit),
    staleTime: 5 * 60 * 1000,
  });
}

export function useRecommendedRooms(limit = 6) {
  return useQuery({
    queryKey: queryKeys.recommendations.rooms(limit),
    queryFn: () => getRecommendedRooms(limit),
    staleTime: 5 * 60 * 1000,
  });
}
