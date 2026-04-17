import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { queryKeys } from '@/lib/queryKeys';
import { getGuides, getGuideDetail, createGuide } from './api';
import type { CreateGuidePayload } from './types';

export function useGuides(gameId?: string, sortBy = 'recent', page = 1) {
  return useQuery({
    queryKey: queryKeys.guides.list(gameId, sortBy, page),
    queryFn: () => getGuides({ gameId, sortBy, page }),
  });
}

export function useGuideDetail(guideId: string | undefined) {
  return useQuery({
    queryKey: queryKeys.guides.detail(guideId ?? ''),
    queryFn: () => getGuideDetail(guideId!),
    enabled: Boolean(guideId),
  });
}

export function useCreateGuide() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (payload: CreateGuidePayload) => createGuide(payload),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ['guides'] });
    },
  });
}
