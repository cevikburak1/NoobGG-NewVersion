import { z } from 'zod';

export const privacySettingsSchema = z.object({
  profileVisibility: z.enum(['Public', 'FriendsOnly', 'Private']),
  dmPermission: z.enum(['Everyone', 'FriendsOnly', 'Nobody']),
  showOnlineStatus: z.boolean(),
  defaultLookingForTeam: z.boolean(),
});

export type PrivacySettingsFormData = z.infer<typeof privacySettingsSchema>;

export const notificationSettingsSchema = z.object({
  notifyFriendRequests: z.boolean(),
  notifyDirectMessages: z.boolean(),
  notifyRoomActivity: z.boolean(),
  notifySystemMessages: z.boolean(),
});

export type NotificationSettingsFormData = z.infer<typeof notificationSettingsSchema>;

export const deactivateSchema = z.object({
  reason: z.string().max(500).optional(),
});

export type DeactivateFormData = z.infer<typeof deactivateSchema>;
