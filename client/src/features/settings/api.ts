import { api } from '@/lib/api';
import type {
  DeactivateRequest,
  UpdateNotificationRequest,
  UpdatePrivacyRequest,
  UserSettingsResponse,
} from './types';

export async function getSettings(): Promise<UserSettingsResponse> {
  const { data } = await api.get<UserSettingsResponse>('/api/settings');
  return data;
}

export async function updatePrivacy(payload: UpdatePrivacyRequest): Promise<UserSettingsResponse> {
  const { data } = await api.put<UserSettingsResponse>('/api/settings/privacy', payload);
  return data;
}

export async function updateNotifications(
  payload: UpdateNotificationRequest,
): Promise<UserSettingsResponse> {
  const { data } = await api.put<UserSettingsResponse>('/api/settings/notifications', payload);
  return data;
}

export async function deactivateAccount(payload: DeactivateRequest): Promise<void> {
  await api.post('/api/settings/deactivate', payload);
}

export async function reactivateAccount(): Promise<void> {
  await api.post('/api/settings/reactivate');
}

export async function requestAccountDeletion(): Promise<void> {
  await api.post('/api/settings/request-deletion');
}
