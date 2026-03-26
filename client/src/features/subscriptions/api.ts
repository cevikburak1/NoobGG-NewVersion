import { api } from '@/lib/api';
import type {
  CancelSubscriptionRequest,
  PlanComparisonResponse,
  UserSubscriptionResponse,
} from './types';

export async function getPlans(): Promise<PlanComparisonResponse> {
  const { data } = await api.get<PlanComparisonResponse>('/api/subscriptions/plans');
  return data;
}

export async function getMySubscription(): Promise<UserSubscriptionResponse> {
  const { data } = await api.get<UserSubscriptionResponse>('/api/subscriptions/me');
  return data;
}

export async function cancelSubscription(body: CancelSubscriptionRequest): Promise<void> {
  await api.post('/api/subscriptions/cancel', body);
}
