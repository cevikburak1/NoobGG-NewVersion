import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { queryKeys } from '@/lib/queryKeys';
import {
  deactivateAccount,
  getSettings,
  reactivateAccount,
  requestAccountDeletion,
  updateNotifications,
  updatePrivacy,
} from './api';
import type { DeactivateRequest, UpdateNotificationRequest, UpdatePrivacyRequest } from './types';

export function useSettings() {
  return useQuery({
    queryKey: queryKeys.settings.me(),
    queryFn: getSettings,
  });
}

export function useUpdatePrivacy() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (data: UpdatePrivacyRequest) => updatePrivacy(data),
    onSuccess: (updated) => {
      queryClient.setQueryData(queryKeys.settings.me(), updated);
    },
  });
}

export function useUpdateNotifications() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (data: UpdateNotificationRequest) => updateNotifications(data),
    onSuccess: (updated) => {
      queryClient.setQueryData(queryKeys.settings.me(), updated);
    },
  });
}

export function useDeactivateAccount() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (data: DeactivateRequest) => deactivateAccount(data),
    onSuccess: async () => {
      await queryClient.invalidateQueries({ queryKey: queryKeys.settings.me() });
    },
  });
}

export function useReactivateAccount() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: reactivateAccount,
    onSuccess: async () => {
      await queryClient.invalidateQueries({ queryKey: queryKeys.settings.me() });
    },
  });
}

export function useRequestDeletion() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: requestAccountDeletion,
    onSuccess: async () => {
      await queryClient.invalidateQueries({ queryKey: queryKeys.settings.me() });
    },
  });
}
