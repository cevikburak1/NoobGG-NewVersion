import type { DmPermission, ProfileVisibility } from '@/types/enums';

export interface UserSettingsResponse {
  profileVisibility: ProfileVisibility;
  dmPermission: DmPermission;
  showOnlineStatus: boolean;
  defaultLookingForTeam: boolean;
  notifyFriendRequests: boolean;
  notifyDirectMessages: boolean;
  notifyRoomActivity: boolean;
  notifySystemMessages: boolean;
  isDeactivated: boolean;
  deactivatedAt: string | null;
  deletionRequestedAt: string | null;
}

export interface UpdatePrivacyRequest {
  profileVisibility: ProfileVisibility;
  dmPermission: DmPermission;
  showOnlineStatus: boolean;
  defaultLookingForTeam: boolean;
}

export interface UpdateNotificationRequest {
  notifyFriendRequests: boolean;
  notifyDirectMessages: boolean;
  notifyRoomActivity: boolean;
  notifySystemMessages: boolean;
}

export interface DeactivateRequest {
  reason?: string;
}
