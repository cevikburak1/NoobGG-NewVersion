import type { Region, Language, RoomStatus, ReportStatus, ReportTargetType, ReportReason } from './enums';

export interface ApiError {
  type: string;
  title: string;
  status: number;
}

export interface PagedResult<T> {
  items: T[];
  totalCount: number;
  page: number;
  pageSize: number;
  hasNextPage: boolean;
  hasPreviousPage: boolean;
}

export interface RoomFilters {
  gameId?: string;
  region?: Region;
  language?: Language;
  tag?: string;
  status?: RoomStatus;
  page?: number;
  pageSize?: number;
}

export interface GuildFilters {
  gameId?: string;
  region?: Region;
  language?: Language;
  search?: string;
  page?: number;
  pageSize?: number;
}

export interface ReportFilters {
  status?: ReportStatus;
  targetType?: ReportTargetType;
  reason?: ReportReason;
  page?: number;
  pageSize?: number;
}
