import { api } from '@/lib/api';
import type {
  AddGameProfileRequest,
  GameProfileResponse,
  ProfileDetailResponse,
  UpdateGameProfileRequest,
  UpdateProfileRequest,
} from './types';

export async function getProfile(userId: string): Promise<ProfileDetailResponse> {
  const { data } = await api.get<ProfileDetailResponse>(`/api/profiles/${userId}`);
  return data;
}

export async function getMyProfile(): Promise<ProfileDetailResponse> {
  const { data } = await api.get<ProfileDetailResponse>('/api/profiles/me');
  return data;
}

export async function updateProfile(body: UpdateProfileRequest): Promise<ProfileDetailResponse> {
  const { data } = await api.put<ProfileDetailResponse>('/api/profiles/me', body);
  return data;
}

export async function getGameProfiles(userId: string): Promise<GameProfileResponse[]> {
  const { data } = await api.get<GameProfileResponse[]>(`/api/profiles/${userId}/games`);
  return data;
}

export async function addGameProfile(body: AddGameProfileRequest): Promise<GameProfileResponse> {
  const { data } = await api.post<GameProfileResponse>('/api/profiles/me/games', body);
  return data;
}

export async function updateGameProfile(
  id: string,
  body: UpdateGameProfileRequest,
): Promise<GameProfileResponse> {
  const { data } = await api.put<GameProfileResponse>(`/api/profiles/me/games/${id}`, body);
  return data;
}

export async function deleteGameProfile(id: string): Promise<void> {
  await api.delete(`/api/profiles/me/games/${id}`);
}

export async function uploadAvatar(file: File): Promise<ProfileDetailResponse> {
  const formData = new FormData();
  formData.append('file', file);
  const { data } = await api.post<ProfileDetailResponse>('/api/profiles/me/avatar', formData, {
    headers: { 'Content-Type': 'multipart/form-data' },
  });
  return data;
}

export async function uploadBanner(file: File): Promise<ProfileDetailResponse> {
  const formData = new FormData();
  formData.append('file', file);
  const { data } = await api.post<ProfileDetailResponse>('/api/profiles/me/banner', formData, {
    headers: { 'Content-Type': 'multipart/form-data' },
  });
  return data;
}

export async function removeBanner(): Promise<ProfileDetailResponse> {
  const { data } = await api.delete<ProfileDetailResponse>('/api/profiles/me/banner');
  return data;
}
