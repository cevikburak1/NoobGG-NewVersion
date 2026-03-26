import { motion } from 'framer-motion';
import { cn } from '@/lib/cn';

interface ProgressBarProps {
  value: number;
  max?: number;
  variant?: 'primary' | 'accent' | 'warning' | 'danger';
  size?: 'sm' | 'md';
  className?: string;
}

const variantStyles = {
  primary: 'bg-primary',
  accent: 'bg-accent',
  warning: 'bg-warning',
  danger: 'bg-danger',
};

export function ProgressBar({ value, max = 100, variant = 'primary', size = 'md', className }: ProgressBarProps) {
  const percentage = Math.min((value / max) * 100, 100);

  return (
    <div
      className={cn(
        'w-full overflow-hidden rounded-full bg-surface-hover',
        size === 'sm' ? 'h-1.5' : 'h-2.5',
        className,
      )}
    >
      <motion.div
        initial={{ width: 0 }}
        animate={{ width: `${percentage}%` }}
        transition={{ duration: 0.8, ease: 'easeOut' }}
        className={cn('h-full rounded-full', variantStyles[variant])}
      />
    </div>
  );
}
