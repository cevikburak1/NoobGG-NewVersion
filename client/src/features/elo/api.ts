import { api } from '@/lib/api';
import type { PagedResult } from '@/types/api';
import type { LeaderboardEntry, EloHistoryResponse, RecordMatchRequest, SubmitSessionResultsRequest } from './types';

export async function getLeaderboard(
  gameId: string,
  page = 1,
  pageSize = 50,
): Promise<PagedResult<LeaderboardEntry>> {
  const { data } = await api.get<PagedResult<LeaderboardEntry>>('/api/elo/leaderboard', {
    params: { gameId, page, pageSize },
  });
  return data;
}

export async function getEloHistory(userId: string, gameId: string): Promise<EloHistoryResponse> {
  const { data } = await api.get<EloHistoryResponse>(`/api/elo/history/${userId}/${gameId}`);
  return data;
}

export async function recordMatch(request: RecordMatchRequest): Promise<void> {
  await api.post('/api/elo/match', request);
}

export async function submitSessionResults(request: SubmitSessionResultsRequest): Promise<void> {
  await api.post('/api/elo/session-results', request);
}
