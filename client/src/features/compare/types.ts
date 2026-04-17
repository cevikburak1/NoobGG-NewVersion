import type { GameProfileResponse, ProfileDetailResponse } from '@/features/profile/types';

/** URL query: `/compare?left=<userId>&right=<userId>` */
export const COMPARE_QUERY_LEFT = 'left';
export const COMPARE_QUERY_RIGHT = 'right';

export type CompareSlot = 'left' | 'right';

export interface SharedGameCompareRow {
  gameId: string;
  gameName: string;
  gameImageUrl: string | null;
  left: Pick<GameProfileResponse, 'eloPoints' | 'rank' | 'rankTier' | 'role' | 'region'>;
  right: Pick<GameProfileResponse, 'eloPoints' | 'rank' | 'rankTier' | 'role' | 'region'>;
  eloDiff: number;
}

export interface CompareHeadlineStats {
  totalGamesLeft: number;
  totalGamesRight: number;
  sharedGameCount: number;
  avgEloLeft: number | null;
  avgEloRight: number | null;
}

export interface CompareViewModel {
  left: ProfileDetailResponse;
  right: ProfileDetailResponse;
  sharedRows: SharedGameCompareRow[];
  onlyLeftGames: GameProfileResponse[];
  onlyRightGames: GameProfileResponse[];
  headline: CompareHeadlineStats;
}
