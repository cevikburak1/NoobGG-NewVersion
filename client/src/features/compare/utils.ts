import type { GameProfileResponse, ProfileDetailResponse } from '@/features/profile/types';
import type { CompareHeadlineStats, CompareViewModel, SharedGameCompareRow } from './types';

function gameMap(games: GameProfileResponse[]): Map<string, GameProfileResponse> {
  const m = new Map<string, GameProfileResponse>();
  for (const g of games) m.set(g.gameId, g);
  return m;
}

export function buildSharedGameRows(
  leftGames: GameProfileResponse[],
  rightGames: GameProfileResponse[],
): SharedGameCompareRow[] {
  const rightById = gameMap(rightGames);
  const rows: SharedGameCompareRow[] = [];

  for (const lg of leftGames) {
    const rg = rightById.get(lg.gameId);
    if (!rg) continue;
    rows.push({
      gameId: lg.gameId,
      gameName: lg.gameName || rg.gameName,
      gameImageUrl: lg.gameImageUrl ?? rg.gameImageUrl,
      left: {
        eloPoints: lg.eloPoints,
        rank: lg.rank,
        rankTier: lg.rankTier,
        role: lg.role,
        region: lg.region,
      },
      right: {
        eloPoints: rg.eloPoints,
        rank: rg.rank,
        rankTier: rg.rankTier,
        role: rg.role,
        region: rg.region,
      },
      eloDiff: lg.eloPoints - rg.eloPoints,
    });
  }

  return rows.sort((a, b) => Math.max(b.left.eloPoints, b.right.eloPoints) - Math.max(a.left.eloPoints, a.right.eloPoints));
}

export function onlyOnSide(
  sideGames: GameProfileResponse[],
  otherGames: GameProfileResponse[],
): GameProfileResponse[] {
  const otherIds = new Set(otherGames.map((g) => g.gameId));
  return sideGames.filter((g) => !otherIds.has(g.gameId));
}

export function buildCompareViewModel(
  left: ProfileDetailResponse,
  right: ProfileDetailResponse,
): CompareViewModel {
  const sharedRows = buildSharedGameRows(left.games, right.games);
  return {
    left,
    right,
    sharedRows,
    onlyLeftGames: onlyOnSide(left.games, right.games),
    onlyRightGames: onlyOnSide(right.games, left.games),
    headline: buildHeadlineStats(left, right, sharedRows.length),
  };
}

export function buildHeadlineStats(
  left: ProfileDetailResponse | undefined,
  right: ProfileDetailResponse | undefined,
  sharedCount: number,
): CompareHeadlineStats {
  const lg = left?.games ?? [];
  const rg = right?.games ?? [];
  const avg = (games: GameProfileResponse[]) =>
    games.length === 0 ? null : Math.round(games.reduce((s, g) => s + g.eloPoints, 0) / games.length);

  return {
    totalGamesLeft: lg.length,
    totalGamesRight: rg.length,
    sharedGameCount: sharedCount,
    avgEloLeft: avg(lg),
    avgEloRight: avg(rg),
  };
}
