import { motion } from 'framer-motion';
import { cn } from '@/lib/cn';
import { useToggleVote } from '@/features/community/hooks';

interface UpvoteButtonProps {
  targetId: string;
  targetType: number;
  count: number;
  hasUpvoted: boolean;
  size?: 'sm' | 'md';
}

const sizeConfig = {
  sm: { button: 'gap-1 px-2 py-1', icon: 'h-3.5 w-3.5', text: 'text-xs' },
  md: { button: 'gap-1.5 px-2.5 py-1.5', icon: 'h-4 w-4', text: 'text-sm' },
};

export function UpvoteButton({ targetId, targetType, count, hasUpvoted, size = 'md' }: UpvoteButtonProps) {
  const { mutate, isPending } = useToggleVote();
  const cfg = sizeConfig[size];

  const handleClick = () => {
    if (isPending) return;
    mutate({ targetId, targetType });
  };

  return (
    <motion.button
      type="button"
      onClick={handleClick}
      disabled={isPending}
      whileTap={{ scale: 0.9 }}
      className={cn(
        'inline-flex items-center rounded-lg font-medium transition-colors',
        'focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-primary/50',
        'disabled:opacity-60',
        cfg.button,
        hasUpvoted
          ? 'bg-primary/15 text-primary border border-primary/30'
          : 'bg-surface-hover/60 text-foreground-muted border border-transparent hover:border-border/50 hover:text-foreground',
      )}
    >
      <motion.svg
        viewBox="0 0 24 24"
        fill={hasUpvoted ? 'currentColor' : 'none'}
        stroke="currentColor"
        strokeWidth={hasUpvoted ? 0 : 2}
        strokeLinecap="round"
        strokeLinejoin="round"
        className={cfg.icon}
        animate={hasUpvoted ? { scale: [1, 1.3, 1] } : { scale: 1 }}
        transition={{ type: 'spring', stiffness: 400, damping: 12 }}
      >
        <path d="M12 4l-1.41 1.41L16.17 11H4v2h12.17l-5.58 5.59L12 20l8-8-8-8z" transform="rotate(-90 12 12)" />
      </motion.svg>
      <span className={cfg.text}>{count}</span>
    </motion.button>
  );
}
