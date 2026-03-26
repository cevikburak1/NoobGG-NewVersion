import type { ReactNode } from 'react';
import { cn } from '@/lib/cn';

type BadgeVariant = 'default' | 'primary' | 'accent' | 'danger' | 'warning' | 'success';

interface BadgeProps {
  variant?: BadgeVariant;
  children: ReactNode;
  className?: string;
}

const variantStyles: Record<BadgeVariant, string> = {
  default: 'bg-surface-hover text-foreground-muted',
  primary: 'bg-primary/20 text-primary-hover',
  accent: 'bg-accent/20 text-accent',
  danger: 'bg-danger/20 text-danger',
  warning: 'bg-warning/20 text-warning',
  success: 'bg-success/20 text-success',
};

export function Badge({ variant = 'default', children, className }: BadgeProps) {
  return (
    <span
      className={cn(
        'inline-flex items-center rounded-full px-2.5 py-0.5 text-xs font-medium',
        variantStyles[variant],
        className,
      )}
    >
      {children}
    </span>
  );
}
