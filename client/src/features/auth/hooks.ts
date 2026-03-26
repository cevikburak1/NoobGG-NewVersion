import { useMutation, useQuery } from '@tanstack/react-query';
import * as authApi from '@/features/auth/api';
import { queryKeys } from '@/lib/queryKeys';
import { useAuthStore } from '@/stores/authStore';

export function useLogin() {
  return useMutation({
    mutationFn: authApi.login,
    onSuccess: (data) => {
      useAuthStore.getState().login(data.user, data.accessToken, data.refreshToken);
    },
  });
}

export function useRegister() {
  return useMutation({
    mutationFn: authApi.register,
  });
}

export function useVerifyEmail() {
  return useMutation({
    mutationFn: authApi.verifyEmail,
    onSuccess: (data) => {
      useAuthStore.getState().login(data.user, data.accessToken, data.refreshToken);
    },
  });
}

export function useResendVerification() {
  return useMutation({
    mutationFn: authApi.resendVerificationEmail,
  });
}

export function useLogout() {
  return useMutation({
    mutationFn: authApi.logout,
    onSuccess: () => {
      useAuthStore.getState().logout();
    },
  });
}

export function useMe() {
  const refreshToken = useAuthStore((s) => s.refreshToken);
  return useQuery({
    queryKey: queryKeys.auth.me(),
    queryFn: authApi.getMe,
    enabled: Boolean(refreshToken),
  });
}
