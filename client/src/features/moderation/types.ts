import type { ReportReason, ReportStatus, ReportTargetType } from '@/types/enums';

export interface ReportResponse {
  id: string;
  targetType: ReportTargetType;
  reportedUserId: string;
  reportedUsername: string | null;
  roomId: string | null;
  roomTitle: string | null;
  reason: ReportReason;
  description: string | null;
  status: ReportStatus;
  createdAt: string;
}

export interface ReportDetailResponse {
  id: string;
  reporterId: string;
  reporterUsername: string | null;
  targetType: ReportTargetType;
  reportedUserId: string;
  reportedUsername: string | null;
  roomId: string | null;
  roomTitle: string | null;
  reason: ReportReason;
  description: string | null;
  status: ReportStatus;
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

export type ReviewAction = 'warn' | 'suspend' | 'ban' | 'dismiss';

export type SuspensionDuration = '1d' | '7d' | '30d';
