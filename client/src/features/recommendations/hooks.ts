import { useQuery } from '@tanstack/react-query';
import { queryKeys } from '@/lib/queryKeys';
import {
  getRecommendedPlayers,
  getRecommendedRooms,
  getAiRecommendedPlayers,
  getRecentPlayers,
  getRecentRooms,
} from './api';

export function useRecommendedPlayers(limit = 6) {
  return useQuery({
    queryKey: queryKeys.recommendations.players(limit),
    queryFn: () => getRecommendedPlayers(limit),
    staleTime: 5 * 60 * 1000,
  });
}

export function useAiRecommendedPlayers(limit = 10, enabled = true) {
  return useQuery({
    queryKey: queryKeys.recommendations.playersAi(limit),
    queryFn: () => getAiRecommendedPlayers(limit),
    staleTime: 5 * 60 * 1000,
    enabled,
  });
}

export function useRecommendedRooms(limit = 6) {
  return useQuery({
    queryKey: queryKeys.recommendations.rooms(limit),
    queryFn: () => getRecommendedRooms(limit),
    staleTime: 5 * 60 * 1000,
  });
}

export function useRecentPlayers(limit = 5) {
  return useQuery({
    queryKey: queryKeys.recent.players(limit),
    queryFn: () => getRecentPlayers(limit),
    staleTime: 2 * 60 * 1000,
  });
}

export function useRecentRooms(limit = 5) {
  return useQuery({
    queryKey: queryKeys.recent.rooms(limit),
    queryFn: () => getRecentRooms(limit),
    staleTime: 2 * 60 * 1000,
  });
}
