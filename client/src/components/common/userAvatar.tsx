import { cn } from '@/lib/cn';
import { resolveFileUrl } from '@/lib/api';

interface UserAvatarProps {
  username: string;
  avatarUrl?: string | null;
  size?: 'xs' | 'sm' | 'md' | 'lg';
  className?: string;
}

const sizeStyles = {
  xs: 'h-6 w-6 text-[10px]',
  sm: 'h-8 w-8 text-xs',
  md: 'h-10 w-10 text-sm',
  lg: 'h-14 w-14 text-lg',
};

export function UserAvatar({ username, avatarUrl, size = 'md', className }: UserAvatarProps) {
  const initials = username.slice(0, 2).toUpperCase();
  const resolvedUrl = resolveFileUrl(avatarUrl);

  if (resolvedUrl) {
    return (
      <img
        src={resolvedUrl}
        alt={username}
        className={cn('rounded-full object-cover', sizeStyles[size], className)}
      />
    );
  }

  return (
    <div
      className={cn(
        'flex items-center justify-center rounded-full bg-primary/20 font-semibold text-primary',
        sizeStyles[size],
        className,
      )}
    >
      {initials}
    </div>
  );
}
