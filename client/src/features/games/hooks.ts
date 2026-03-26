import { useQuery } from '@tanstack/react-query';
import { queryKeys } from '@/lib/queryKeys';
import { searchGames, browseGames } from './api';
import type { GameSearchParams, GameBrowseParams } from './types';

export function useGameSearch(query: string, params?: Omit<GameSearchParams, 'q'>) {
  return useQuery({
    queryKey: queryKeys.games.search(query),
    queryFn: () => searchGames({ q: query, ...params }),
    enabled: query.length >= 2,
  });
}

export function useGameBrowse(params: GameBrowseParams) {
  return useQuery({
    queryKey: queryKeys.games.browse(params),
    queryFn: () => browseGames(params),
  });
}
