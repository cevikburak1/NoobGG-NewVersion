import { api } from '@/lib/api';
import type { NotificationResponse } from './types';

export async function getNotifications(): Promise<NotificationResponse[]> {
  const { data } = await api.get<NotificationResponse[]>('/api/notifications');
  return data;
}

export async function markAsRead(id: string): Promise<void> {
  await api.post(`/api/notifications/${id}/read`);
}

export async function markAllAsRead(): Promise<void> {
  await api.post('/api/notifications/read-all');
}
