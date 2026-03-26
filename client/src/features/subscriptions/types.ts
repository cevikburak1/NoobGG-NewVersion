export interface PlanResponse {
  id: string;
  name: string;
  description: string;
  tier: string;
  price: number;
  currency: string;
  intervalMonths: number;
  features: string[];
  maxRoomsPerDay: number;
  maxGameProfiles: number;
  isHighlighted: boolean;
  sortOrder: number;
}

export interface PlanComparisonResponse {
  plans: PlanResponse[];
  currentTier: string;
  currentPlanId: string | null;
}

export interface CancelSubscriptionRequest {
  targetUserId?: string;
  immediate: boolean;
}

export interface UserSubscriptionResponse {
  subscriptionId: string | null;
  tier: string;
  planName: string;
  status: string;
  startDate: string | null;
  endDate: string | null;
  autoRenew: boolean;
  entitlements: {
    tier: string;
    planName: string;
    features: string[];
    maxRoomsPerDay: number;
    maxGameProfiles: number;
    hasPremiumBadge: boolean;
  };
}
