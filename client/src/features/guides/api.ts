import { api } from '@/lib/api';
import type { GuideListResponse, GuideDetailResponse, CreateGuidePayload } from './types';

export async function getGuides(params: {
  gameId?: string;
  page?: number;
  pageSize?: number;
  sortBy?: string;
}): Promise<GuideListResponse> {
  const { data } = await api.get<GuideListResponse>('/api/guides', { params });
  return data;
}

export async function getGuideDetail(guideId: string): Promise<GuideDetailResponse> {
  const { data } = await api.get<GuideDetailResponse>(`/api/guides/${guideId}`);
  return data;
}

export async function createGuide(payload: CreateGuidePayload): Promise<GuideDetailResponse> {
  const { data } = await api.post<GuideDetailResponse>('/api/guides', payload);
  return data;
}
