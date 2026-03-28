import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { queryKeys } from '@/lib/queryKeys';
import {
  addGameProfile,
  deleteGameProfile,
  getGameProfiles,
  getMyProfile,
  getProfile,
  removeBanner,
  updateGameProfile,
  updateProfile,
  uploadAvatar,
  uploadBanner,
} from './api';
import type {
  AddGameProfileRequest,
  UpdateGameProfileRequest,
  UpdateProfileRequest,
} from './types';

export function useProfile(userId: string | null | undefined) {
  return useQuery({
    queryKey: userId ? queryKeys.profile.detail(userId) : (['profile', 'detail', 'none'] as const),
    queryFn: () => getProfile(userId!),
    enabled: Boolean(userId),
  });
}

export function useMyProfile() {
  return useQuery({
    queryKey: queryKeys.profile.me(),
    queryFn: getMyProfile,
  });
}

export function useUpdateProfile() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (data: UpdateProfileRequest) => updateProfile(data),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ['profile'] });
    },
  });
}

export function useGameProfiles(userId: string | null | undefined) {
  return useQuery({
    queryKey: userId
      ? queryKeys.profile.gameProfiles(userId)
      : (['profile', 'games', 'none'] as const),
    queryFn: () => getGameProfiles(userId!),
    enabled: Boolean(userId),
  });
}

export function useAddGameProfile() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (data: AddGameProfileRequest) => addGameProfile(data),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ['profile'] });
    },
  });
}

export function useUpdateGameProfile() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: ({ id, ...data }: UpdateGameProfileRequest & { id: string }) =>
      updateGameProfile(id, data),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ['profile'] });
    },
  });
}

export function useDeleteGameProfile() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (id: string) => deleteGameProfile(id),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ['profile'] });
    },
  });
}

export function useUploadAvatar() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (file: File) => uploadAvatar(file),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ['profile'] });
    },
  });
}

export function useUploadBanner() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (file: File) => uploadBanner(file),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ['profile'] });
    },
  });
}

export function useRemoveBanner() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: () => removeBanner(),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ['profile'] });
    },
  });
}
