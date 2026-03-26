import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { queryKeys } from '@/lib/queryKeys';
import type { ReportFilters } from '@/types/api';
import { createReport, getReportDetail, getReports, reviewReport } from './api';
import type { CreateReportRequest, ReviewReportRequest } from './types';

export function useReports(filters: ReportFilters) {
  return useQuery({
    queryKey: queryKeys.moderation.reports(filters),
    queryFn: () => getReports(filters),
  });
}

export function useReportDetail(id: string | undefined) {
  return useQuery({
    queryKey: queryKeys.moderation.reportDetail(id ?? ''),
    queryFn: () => {
      if (!id) {
        throw new Error('Report id is required');
      }
      return getReportDetail(id);
    },
    enabled: Boolean(id),
  });
}

export function useReviewReport() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: ({
      id,
      data,
    }: {
      id: string;
      data: ReviewReportRequest;
    }) => reviewReport(id, data),
    onSuccess: async () => {
      await queryClient.invalidateQueries({ queryKey: ['moderation', 'reports'] });
    },
  });
}

export function useCreateReport() {
  return useMutation({
    mutationFn: (data: CreateReportRequest) => createReport(data),
  });
}
