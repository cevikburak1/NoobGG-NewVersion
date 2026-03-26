import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { queryKeys } from '@/lib/queryKeys';
import { useAuthStore } from '@/stores/authStore';
import { cancelSubscription, getMySubscription, getPlans } from './api';
import type { CancelSubscriptionRequest } from './types';

export function usePlans() {
  return useQuery({
    queryKey: queryKeys.subscriptions.plans(),
    queryFn: getPlans,
    staleTime: Infinity,
  });
}

export function useMySubscription() {
  const authenticated = useAuthStore((s) => s.accessToken !== null && s.user !== null);

  return useQuery({
    queryKey: queryKeys.subscriptions.me(),
    queryFn: getMySubscription,
    enabled: authenticated,
  });
}

export function useCancelSubscription() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (data: CancelSubscriptionRequest) => cancelSubscription(data),
    onSuccess: async () => {
      await queryClient.invalidateQueries({ queryKey: queryKeys.subscriptions.me() });
    },
  });
}
