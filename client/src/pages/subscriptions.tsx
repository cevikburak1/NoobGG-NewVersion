import { useState } from 'react';
import { motion, AnimatePresence } from 'framer-motion';
import { usePlans, useMySubscription, useCancelSubscription } from '@/features/subscriptions/hooks';
import { useAuthStore } from '@/stores/authStore';
import type { PlanResponse } from '@/features/subscriptions/types';
import { Button, Modal, AnimatedPage, Spinner, staggerContainer, staggerItem } from '@/components/ui';

const tierIcons: Record<string, string> = {
  Free: '🎮',
  Plus: '⚡',
  Pro: '💎',
};

const tierGradients: Record<string, string> = {
  Free: 'from-foreground-subtle/10 to-transparent',
  Plus: 'from-primary/20 to-primary/5',
  Pro: 'from-accent/20 to-accent/5',
};

export default function SubscriptionsPage() {
  const isAuth = useAuthStore((s) => s.isAuthenticated());
  const { data: planData, isLoading: plansLoading } = usePlans();
  const { data: subscription } = useMySubscription();
  const cancelSubscription = useCancelSubscription();
  const [showCancelModal, setShowCancelModal] = useState(false);
  const [billingPeriod, setBillingPeriod] = useState<'monthly' | 'yearly'>('monthly');

  if (plansLoading) {
    return (
      <div className="flex items-center justify-center py-32">
        <Spinner size="lg" />
      </div>
    );
  }

  const plans = planData?.plans
    ? [...planData.plans].sort((a, b) => a.sortOrder - b.sortOrder)
    : mockPlans;

  const currentTier = subscription?.tier ?? planData?.currentTier ?? 'Free';

  return (
    <AnimatedPage>
      <div className="space-y-8">
        {/* Header */}
        <motion.div
          initial={{ opacity: 0, y: 20 }}
          animate={{ opacity: 1, y: 0 }}
          className="text-center"
        >
          <motion.div
            initial={{ scale: 0 }}
            animate={{ scale: 1 }}
            transition={{ type: 'spring', bounce: 0.5, delay: 0.1 }}
            className="mx-auto mb-4 inline-flex h-16 w-16 items-center justify-center rounded-2xl bg-gradient-to-br from-primary to-accent text-2xl shadow-lg shadow-primary/25"
          >
            ✨
          </motion.div>
          <h1 className="text-3xl font-bold text-foreground sm:text-4xl">
            Choose Your <span className="text-primary">Plan</span>
          </h1>
          <p className="mx-auto mt-3 max-w-lg text-foreground-muted">
            Unlock premium features and take your gaming experience to the next level
          </p>
        </motion.div>

        {/* Billing toggle */}
        <motion.div
          initial={{ opacity: 0 }}
          animate={{ opacity: 1 }}
          transition={{ delay: 0.2 }}
          className="flex items-center justify-center gap-3"
        >
          <button
            onClick={() => setBillingPeriod('monthly')}
            className={`rounded-lg px-4 py-2 text-sm font-medium transition-colors ${
              billingPeriod === 'monthly'
                ? 'bg-primary text-primary-foreground'
                : 'text-foreground-muted hover:text-foreground'
            }`}
          >
            Monthly
          </button>
          <button
            onClick={() => setBillingPeriod('yearly')}
            className={`relative rounded-lg px-4 py-2 text-sm font-medium transition-colors ${
              billingPeriod === 'yearly'
                ? 'bg-primary text-primary-foreground'
                : 'text-foreground-muted hover:text-foreground'
            }`}
          >
            Yearly
            <motion.span
              initial={{ opacity: 0, scale: 0.8 }}
              animate={{ opacity: 1, scale: 1 }}
              className="absolute -right-2 -top-2 rounded-full bg-accent px-1.5 py-0.5 text-[10px] font-bold text-accent-foreground"
            >
              -20%
            </motion.span>
          </button>
        </motion.div>

        {/* Current plan indicator */}
        {isAuth && subscription && (
          <motion.div
            initial={{ opacity: 0, y: 10 }}
            animate={{ opacity: 1, y: 0 }}
            transition={{ delay: 0.25 }}
            className="mx-auto max-w-sm rounded-xl border border-primary/30 bg-primary/5 p-4 text-center"
          >
            <p className="text-sm text-foreground-muted">
              You&apos;re currently on the{' '}
              <span className="font-semibold text-primary">{subscription.planName}</span> plan
            </p>
            {subscription.endDate && (
              <p className="mt-1 text-xs text-foreground-subtle">
                {subscription.autoRenew ? 'Renews' : 'Expires'} on{' '}
                {new Date(subscription.endDate).toLocaleDateString()}
              </p>
            )}
          </motion.div>
        )}

        {/* Plan Cards */}
        <motion.div
          variants={staggerContainer}
          initial="hidden"
          animate="show"
          className="mx-auto grid max-w-5xl gap-6 md:grid-cols-3"
        >
          {plans.map((plan, index) => (
            <PlanCard
              key={plan.id}
              plan={plan}
              index={index}
              billingPeriod={billingPeriod}
              isCurrentPlan={plan.tier === currentTier}
              isAuth={isAuth}
              onCancel={() => setShowCancelModal(true)}
            />
          ))}
        </motion.div>

        {/* Feature comparison */}
        <motion.div
          initial={{ opacity: 0, y: 20 }}
          whileInView={{ opacity: 1, y: 0 }}
          viewport={{ once: true }}
          className="mx-auto max-w-3xl"
        >
          <h2 className="mb-6 text-center text-xl font-bold text-foreground">
            Compare All Features
          </h2>
          <FeatureComparisonTable plans={plans} currentTier={currentTier} />
        </motion.div>

        {/* FAQ */}
        <motion.div
          initial={{ opacity: 0, y: 20 }}
          whileInView={{ opacity: 1, y: 0 }}
          viewport={{ once: true }}
          className="mx-auto max-w-2xl"
        >
          <h2 className="mb-6 text-center text-xl font-bold text-foreground">
            Frequently Asked Questions
          </h2>
          <div className="space-y-3">
            {faqs.map((faq, i) => (
              <FaqItem key={i} question={faq.question} answer={faq.answer} index={i} />
            ))}
          </div>
        </motion.div>

        {/* Cancel modal */}
        <Modal
          isOpen={showCancelModal}
          onClose={() => setShowCancelModal(false)}
          title="Cancel Subscription"
        >
          <p className="text-sm text-foreground-muted">
            Are you sure you want to cancel? You&apos;ll lose access to premium features at the end of your billing period.
          </p>
          <div className="mt-4 flex justify-end gap-3">
            <Button variant="ghost" onClick={() => setShowCancelModal(false)}>Keep Plan</Button>
            <Button
              variant="danger"
              isLoading={cancelSubscription.isPending}
              onClick={() => {
                cancelSubscription.mutate({ immediate: false }, {
                  onSuccess: () => setShowCancelModal(false),
                });
              }}
            >
              Cancel Subscription
            </Button>
          </div>
        </Modal>
      </div>
    </AnimatedPage>
  );
}

function PlanCard({
  plan,
  index,
  billingPeriod,
  isCurrentPlan,
  isAuth,
  onCancel,
}: {
  plan: PlanResponse;
  index: number;
  billingPeriod: 'monthly' | 'yearly';
  isCurrentPlan: boolean;
  isAuth: boolean;
  onCancel: () => void;
}) {
  const yearlyPrice = plan.price * 12 * 0.8;
  const displayPrice = billingPeriod === 'yearly'
    ? (yearlyPrice / 12).toFixed(2)
    : plan.price.toFixed(2);

  return (
    <motion.div
      variants={staggerItem}
      whileHover={{ y: -8, scale: 1.02 }}
      transition={{ duration: 0.3 }}
      className={`relative overflow-hidden rounded-2xl border p-6 ${
        plan.isHighlighted
          ? 'border-primary bg-surface shadow-xl shadow-primary/10'
          : 'border-border bg-surface'
      }`}
    >
      {plan.isHighlighted && (
        <motion.div
          initial={{ x: '-100%' }}
          animate={{ x: 0 }}
          transition={{ delay: 0.3 + index * 0.1 }}
          className="absolute left-0 right-0 top-0 bg-gradient-to-r from-primary to-accent py-1 text-center text-xs font-bold text-white"
        >
          MOST POPULAR
        </motion.div>
      )}

      <div className={`absolute inset-0 bg-gradient-to-b ${tierGradients[plan.tier] ?? tierGradients.Free} pointer-events-none`} />

      <div className={`relative ${plan.isHighlighted ? 'pt-4' : ''}`}>
        <div className="text-3xl">{tierIcons[plan.tier] ?? '🎮'}</div>
        <h3 className="mt-3 text-xl font-bold text-foreground">{plan.name}</h3>
        <p className="mt-1 text-sm text-foreground-muted">{plan.description}</p>

        <div className="mt-4 flex items-baseline gap-1">
          {plan.price === 0 ? (
            <span className="text-3xl font-extrabold text-foreground">Free</span>
          ) : (
            <>
              <span className="text-3xl font-extrabold text-foreground">
                ${displayPrice}
              </span>
              <span className="text-sm text-foreground-muted">/month</span>
            </>
          )}
        </div>

        {billingPeriod === 'yearly' && plan.price > 0 && (
          <p className="mt-1 text-xs text-accent">
            Billed ${yearlyPrice.toFixed(2)}/year (save 20%)
          </p>
        )}

        <ul className="mt-6 space-y-3">
          {plan.features.map((feature) => (
            <motion.li
              key={feature}
              initial={{ opacity: 0, x: -10 }}
              whileInView={{ opacity: 1, x: 0 }}
              viewport={{ once: true }}
              className="flex items-start gap-2 text-sm"
            >
              <svg className="mt-0.5 h-4 w-4 shrink-0 text-accent" fill="none" stroke="currentColor" viewBox="0 0 24 24" strokeWidth={2}>
                <path strokeLinecap="round" strokeLinejoin="round" d="M4.5 12.75l6 6 9-13.5" />
              </svg>
              <span className="text-foreground-muted">{feature}</span>
            </motion.li>
          ))}
        </ul>

        <div className="mt-6">
          {isCurrentPlan ? (
            <div className="space-y-2">
              <Button variant="outline" className="w-full" disabled>
                Current Plan
              </Button>
              {plan.price > 0 && (
                <button
                  onClick={onCancel}
                  className="w-full text-xs text-foreground-subtle hover:text-danger transition-colors"
                >
                  Cancel subscription
                </button>
              )}
            </div>
          ) : (
            <motion.div whileHover={{ scale: 1.02 }} whileTap={{ scale: 0.98 }}>
              <Button
                variant={plan.isHighlighted ? 'primary' : 'outline'}
                className="w-full"
              >
                {plan.price === 0 ? 'Get Started' : isAuth ? 'Upgrade' : 'Sign Up'}
              </Button>
            </motion.div>
          )}
        </div>
      </div>
    </motion.div>
  );
}

function FeatureComparisonTable({ plans, currentTier }: { plans: PlanResponse[]; currentTier: string }) {
  const allFeatures = Array.from(new Set(plans.flatMap((p) => p.features)));

  return (
    <div className="overflow-x-auto rounded-xl border border-border">
      <table className="w-full text-sm">
        <thead>
          <tr className="border-b border-border bg-surface">
            <th className="px-4 py-3 text-left text-foreground-muted">Feature</th>
            {plans.map((plan) => (
              <th key={plan.id} className="px-4 py-3 text-center">
                <span className={`font-semibold ${plan.tier === currentTier ? 'text-primary' : 'text-foreground'}`}>
                  {plan.name}
                </span>
              </th>
            ))}
          </tr>
        </thead>
        <tbody>
          <tr className="border-b border-border">
            <td className="px-4 py-3 text-foreground-muted">Rooms per day</td>
            {plans.map((plan) => (
              <td key={plan.id} className="px-4 py-3 text-center font-medium text-foreground">
                {plan.maxRoomsPerDay}
              </td>
            ))}
          </tr>
          <tr className="border-b border-border">
            <td className="px-4 py-3 text-foreground-muted">Game profiles</td>
            {plans.map((plan) => (
              <td key={plan.id} className="px-4 py-3 text-center font-medium text-foreground">
                {plan.maxGameProfiles}
              </td>
            ))}
          </tr>
          {allFeatures.map((feature) => (
            <tr key={feature} className="border-b border-border last:border-0">
              <td className="px-4 py-3 text-foreground-muted">{feature}</td>
              {plans.map((plan) => (
                <td key={plan.id} className="px-4 py-3 text-center">
                  {plan.features.includes(feature) ? (
                    <svg className="mx-auto h-5 w-5 text-accent" fill="none" stroke="currentColor" viewBox="0 0 24 24" strokeWidth={2}>
                      <path strokeLinecap="round" strokeLinejoin="round" d="M4.5 12.75l6 6 9-13.5" />
                    </svg>
                  ) : (
                    <svg className="mx-auto h-5 w-5 text-foreground-subtle" fill="none" stroke="currentColor" viewBox="0 0 24 24" strokeWidth={2}>
                      <path strokeLinecap="round" strokeLinejoin="round" d="M6 18L18 6M6 6l12 12" />
                    </svg>
                  )}
                </td>
              ))}
            </tr>
          ))}
        </tbody>
      </table>
    </div>
  );
}

function FaqItem({ question, answer, index }: { question: string; answer: string; index: number }) {
  const [open, setOpen] = useState(false);

  return (
    <motion.div
      initial={{ opacity: 0, y: 10 }}
      whileInView={{ opacity: 1, y: 0 }}
      viewport={{ once: true }}
      transition={{ delay: index * 0.05 }}
      className="overflow-hidden rounded-xl border border-border bg-surface"
    >
      <button
        onClick={() => setOpen(!open)}
        className="flex w-full items-center justify-between px-5 py-4 text-left text-sm font-medium text-foreground hover:bg-surface-hover transition-colors"
      >
        {question}
        <motion.svg
          animate={{ rotate: open ? 180 : 0 }}
          className="h-4 w-4 shrink-0 text-foreground-muted"
          fill="none"
          stroke="currentColor"
          viewBox="0 0 24 24"
          strokeWidth={2}
        >
          <path strokeLinecap="round" strokeLinejoin="round" d="M19 9l-7 7-7-7" />
        </motion.svg>
      </button>
      <AnimatePresence>
        {open && (
          <motion.div
            initial={{ height: 0, opacity: 0 }}
            animate={{ height: 'auto', opacity: 1 }}
            exit={{ height: 0, opacity: 0 }}
            transition={{ duration: 0.2 }}
          >
            <p className="px-5 pb-4 text-sm text-foreground-muted">{answer}</p>
          </motion.div>
        )}
      </AnimatePresence>
    </motion.div>
  );
}

const mockPlans: PlanResponse[] = [
  {
    id: 'free',
    name: 'Free',
    description: 'Perfect for getting started',
    tier: 'Free',
    price: 0,
    currency: 'USD',
    intervalMonths: 1,
    features: [
      'Join unlimited rooms',
      'Basic profile',
      'Real-time chat',
      'Game search',
    ],
    maxRoomsPerDay: 3,
    maxGameProfiles: 2,
    isHighlighted: false,
    sortOrder: 0,
  },
  {
    id: 'plus',
    name: 'Plus',
    description: 'For dedicated gamers',
    tier: 'Plus',
    price: 4.99,
    currency: 'USD',
    intervalMonths: 1,
    features: [
      'Everything in Free',
      'Create unlimited rooms',
      'Priority matchmaking',
      'Custom room tags',
      'Premium badge',
      'Advanced filters',
    ],
    maxRoomsPerDay: 10,
    maxGameProfiles: 5,
    isHighlighted: true,
    sortOrder: 1,
  },
  {
    id: 'pro',
    name: 'Pro',
    description: 'For competitive teams',
    tier: 'Pro',
    price: 9.99,
    currency: 'USD',
    intervalMonths: 1,
    features: [
      'Everything in Plus',
      'Unlimited rooms per day',
      'Team management tools',
      'Analytics dashboard',
      'Voice channel integration',
      'Priority support',
      'Custom themes',
    ],
    maxRoomsPerDay: 999,
    maxGameProfiles: 20,
    isHighlighted: false,
    sortOrder: 2,
  },
];

const faqs = [
  {
    question: 'Can I change my plan at any time?',
    answer: 'Yes! You can upgrade or downgrade your plan at any time. When upgrading, you\'ll get immediate access to new features. When downgrading, changes take effect at the end of your billing period.',
  },
  {
    question: 'What payment methods do you accept?',
    answer: 'We accept all major credit cards (Visa, Mastercard, Amex), PayPal, and crypto payments. All transactions are secured with industry-standard encryption.',
  },
  {
    question: 'Is there a free trial?',
    answer: 'The Free plan gives you access to core features forever. No credit card required. Try the platform risk-free and upgrade whenever you\'re ready.',
  },
  {
    question: 'What happens if I cancel?',
    answer: 'You\'ll keep your premium features until the end of your billing period. After that, you\'ll be moved to the Free plan. Your data and profiles are always preserved.',
  },
];
