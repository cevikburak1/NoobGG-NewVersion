import { api } from '@/lib/api';
import type { PagedResult, ReportFilters } from '@/types/api';
import type {
  CreateReportRequest,
  ReportDetailResponse,
  ReportResponse,
  ReviewReportRequest,
} from './types';

export async function getReports(
  params: ReportFilters,
): Promise<PagedResult<ReportResponse>> {
  const { data } = await api.get<PagedResult<ReportResponse>>(
    '/api/moderation/reports',
    { params },
  );
  return data;
}

export async function getReportDetail(id: string): Promise<ReportDetailResponse> {
  const { data } = await api.get<ReportDetailResponse>(
    `/api/moderation/reports/${id}`,
  );
  return data;
}

export async function reviewReport(
  id: string,
  data: ReviewReportRequest,
): Promise<void> {
  await api.post(`/api/moderation/reports/${id}/review`, data);
}

export async function createReport(data: CreateReportRequest): Promise<void> {
  await api.post('/api/reports', data);
}
