import type { ReportStatus } from '@/types/enums';

export interface ReportResponse {
  id: string;
  targetType: string;
  reportedUserId: string;
  reportedUsername: string | null;
  roomId: string | null;
  roomTitle: string | null;
  reason: string;
  description: string | null;
  status: string;
  createdAt: string;
}

export interface ReportDetailResponse {
  id: string;
  reporterId: string;
  reporterUsername: string | null;
  targetType: string;
  reportedUserId: string;
  reportedUsername: string | null;
  roomId: string | null;
  roomTitle: string | null;
  reason: string;
  description: string | null;
  status: string;
  reviewedBy: string | null;
  reviewerUsername: string | null;
  reviewNote: string | null;
  reviewedAt: string | null;
  createdAt: string;
}

export interface ReviewReportRequest {
  status: ReportStatus;
  reviewNote?: string;
}

export interface CreateReportRequest {
  targetType: string;
  reportedUserId?: string;
  roomId?: string;
  reason: string;
  description?: string;
}
