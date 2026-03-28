import { api } from '@/lib/api';
import type { FriendResponse, PendingRequestsResponse } from './types';

export async function getFriends(): Promise<FriendResponse[]> {
  const { data } = await api.get<FriendResponse[]>('/api/friends');
  return data;
}

export async function getPendingRequests(): Promise<PendingRequestsResponse> {
  const { data } = await api.get<PendingRequestsResponse>('/api/friends/requests');
  return data;
}

export async function sendFriendRequest(userId: string): Promise<void> {
  await api.post(`/api/friends/request/${userId}`);
}

export async function acceptFriendRequest(friendshipId: string): Promise<void> {
  await api.post(`/api/friends/accept/${friendshipId}`);
}

export async function rejectFriendRequest(friendshipId: string): Promise<void> {
  await api.post(`/api/friends/reject/${friendshipId}`);
}

export async function removeFriend(userId: string): Promise<void> {
  await api.delete(`/api/friends/${userId}`);
}
