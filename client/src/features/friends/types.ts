export interface FriendResponse {
  id: string;
  userId: string;
  username: string;
  avatarUrl: string | null;
  status: string;
  isRequester: boolean;
  createdAt: string;
  respondedAt: string | null;
}

export interface FriendRequestResponse {
  friendshipId: string;
  userId: string;
  username: string;
  avatarUrl: string | null;
  createdAt: string;
}

export interface PendingRequestsResponse {
  incoming: FriendRequestResponse[];
  outgoing: FriendRequestResponse[];
}
