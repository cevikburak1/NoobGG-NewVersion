import { api } from '@/lib/api';
import type { NotificationPagedResult } from './types';

export async function getNotifications(params: {
  unreadOnly?: boolean;
  page?: number;
  pageSize?: number;
}): Promise<NotificationPagedResult> {
  const { data } = await api.get<NotificationPagedResult>('/api/notifications', { params });
  return data;
}

export async function getUnreadCount(): Promise<number> {
  const { data } = await api.get<number>('/api/notifications/unread-count');
  return data;
}

export async function markNotificationRead(id: string): Promise<void> {
  await api.post(`/api/notifications/${id}/read`);
}

export async function markAllNotificationsRead(): Promise<void> {
  await api.post('/api/notifications/read-all');
}
