import { api } from '@/lib/api';
import type { PagedResult, RoomFilters } from '@/types/api';
import type { CreateRoomRequest, RoomDetailResponse, RoomResponse } from '@/features/rooms/types';

export async function getRooms(filters: RoomFilters): Promise<PagedResult<RoomResponse>> {
  const { data } = await api.get<PagedResult<RoomResponse>>('/api/rooms', { params: filters });
  return data;
}

export async function getRoomDetail(id: string): Promise<RoomDetailResponse> {
  const { data } = await api.get<RoomDetailResponse>(`/api/rooms/${id}`);
  return data;
}

export async function createRoom(data: CreateRoomRequest): Promise<RoomDetailResponse> {
  const { data: body } = await api.post<RoomDetailResponse>('/api/rooms', data);
  return body;
}

export async function joinRoom(id: string): Promise<void> {
  await api.post(`/api/rooms/${id}/join`);
}

export async function leaveRoom(id: string): Promise<void> {
  await api.post(`/api/rooms/${id}/leave`);
}

export async function closeRoom(id: string): Promise<void> {
  await api.delete(`/api/rooms/${id}`);
}

export async function kickMember(roomId: string, userId: string): Promise<void> {
  await api.post(`/api/rooms/${roomId}/kick`, { userId });
}
