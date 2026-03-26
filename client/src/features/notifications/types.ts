export type NotificationType =
  | 'FriendRequest'
  | 'FriendAccepted'
  | 'RoomInvite'
  | 'RoomJoined'
  | 'RoomLeft'
  | 'RoomClosed'
  | 'DirectMessage'
  | 'ReportResolved'
  | 'SubscriptionChanged'
  | 'SystemMessage';

export interface NotificationResponse {
  id: string;
  type: NotificationType;
  title: string;
  body: string;
  data: Record<string, string> | null;
  isRead: boolean;
  readAt: string | null;
  createdAt: string;
}

export interface NotificationPagedResult {
  items: NotificationResponse[];
  totalCount: number;
  page: number;
  pageSize: number;
  hasNextPage: boolean;
  hasPreviousPage: boolean;
}
