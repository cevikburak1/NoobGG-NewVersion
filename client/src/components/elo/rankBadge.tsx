const tierStyles: Record<string, string> = {
  Bronze:      'bg-amber-700/20 text-amber-400 border-amber-600/50',
  Silver:      'bg-gray-500/20 text-gray-300 border-gray-500/50',
  Gold:        'bg-yellow-600/20 text-yellow-400 border-yellow-500/50',
  Platinum:    'bg-teal-600/20 text-teal-400 border-teal-500/50',
  Diamond:     'bg-blue-600/20 text-blue-400 border-blue-400/50',
  Master:      'bg-purple-700/20 text-purple-400 border-purple-500/50',
  Grandmaster: 'bg-red-700/20 text-red-400 border-red-500/50',
};

const tierEmoji: Record<string, string> = {
  Bronze:      '🥉',
  Silver:      '🥈',
  Gold:        '🥇',
  Platinum:    '💎',
  Diamond:     '💠',
  Master:      '👑',
  Grandmaster: '🏆',
};

interface RankBadgeProps {
  tier: string;
  eloPoints?: number;
  size?: 'sm' | 'md' | 'lg';
}

export function RankBadge({ tier, eloPoints, size = 'md' }: RankBadgeProps) {
  const style = tierStyles[tier] ?? tierStyles.Bronze;
  const emoji = tierEmoji[tier] ?? '🎮';

  const sizeClasses = {
    sm: 'px-2 py-0.5 text-xs gap-1',
    md: 'px-2.5 py-1 text-sm gap-1.5',
    lg: 'px-3 py-1.5 text-base gap-2',
  };

  return (
    <span className={`inline-flex items-center rounded-full border font-semibold ${style} ${sizeClasses[size]}`}>
      <span>{emoji}</span>
      <span>{tier}</span>
      {eloPoints !== undefined && (
        <span className="opacity-75 font-mono">({eloPoints})</span>
      )}
    </span>
  );
}
