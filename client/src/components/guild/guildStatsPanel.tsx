import { useState, useEffect, useRef } from 'react';
import { Link } from 'react-router-dom';
import { motion } from 'framer-motion';
import {
  Area,
  AreaChart,
  CartesianGrid,
  ResponsiveContainer,
  Tooltip,
  XAxis,
  YAxis,
} from 'recharts';
import { useGuildStats } from '@/features/guildAnalytics/hooks';
import type { GuildActivityPoint, GuildTopPlayerResponse } from '@/features/guildAnalytics/types';
import { Card, CardHeader, CardTitle, Spinner, ProgressBar, staggerContainer, staggerItem } from '@/components/ui';
import { UserAvatar } from '@/components/common/userAvatar';

type Period = 7 | 30 | 90;

interface GuildStatsPanelProps {
  guildId: string;
  canManage: boolean;
}

function useAnimatedCount(target: number, duration = 800) {
  const [value, setValue] = useState(0);
  const prevRef = useRef(0);

  useEffect(() => {
    const start = prevRef.current;
    const diff = target - start;
    if (diff === 0) return;

    const startTime = performance.now();
    let raf: number;

    function tick(now: number) {
      const elapsed = now - startTime;
      const progress = Math.min(elapsed / duration, 1);
      const eased = 1 - Math.pow(1 - progress, 3);
      setValue(Math.round(start + diff * eased));

      if (progress < 1) {
        raf = requestAnimationFrame(tick);
      } else {
        prevRef.current = target;
      }
    }

    raf = requestAnimationFrame(tick);
    return () => cancelAnimationFrame(raf);
  }, [target, duration]);

  return value;
}

interface ActivityTooltipProps {
  active?: boolean;
  payload?: Array<{
    value: number;
    dataKey: string;
    payload: { date: string; matchesPlayed: number; membersJoined: number };
  }>;
}

function ActivityTooltip({ active, payload }: ActivityTooltipProps) {
  if (!active || !payload?.length) return null;
  const data = payload[0].payload;
  return (
    <div className="rounded-lg border border-border bg-surface px-3 py-2 shadow-lg">
      <p className="text-xs font-medium text-foreground-muted mb-1">{data.date}</p>
      <p className="text-sm text-primary font-semibold">{data.matchesPlayed} matches</p>
      <p className="text-sm text-accent font-semibold">{data.membersJoined} joined</p>
    </div>
  );
}

function StatCard({
  label,
  value,
  suffix,
  icon,
  delay,
}: {
  label: string;
  value: number;
  suffix?: string;
  icon: string;
  delay: number;
}) {
  const animated = useAnimatedCount(value);

  return (
    <motion.div
      initial={{ opacity: 0, y: 16 }}
      animate={{ opacity: 1, y: 0 }}
      transition={{ delay, duration: 0.4 }}
      className="rounded-xl border border-border bg-surface/60 backdrop-blur-sm p-4"
    >
      <div className="flex items-center gap-2 mb-2">
        <span className="text-lg">{icon}</span>
        <span className="text-xs font-medium text-foreground-muted uppercase tracking-wide">
          {label}
        </span>
      </div>
      <p className="text-2xl font-bold text-foreground tabular-nums">
        {animated}
        {suffix && <span className="text-base font-medium text-foreground-muted ml-0.5">{suffix}</span>}
      </p>
    </motion.div>
  );
}

function ActivityChart({ timeline }: { timeline: GuildActivityPoint[] }) {
  const chartData = timeline.map((p) => ({
    date: new Date(p.date).toLocaleDateString('en-US', { month: 'short', day: 'numeric' }),
    matchesPlayed: p.matchesPlayed,
    membersJoined: p.membersJoined,
  }));

  if (chartData.length === 0) return null;

  return (
    <ResponsiveContainer width="100%" height={240}>
      <AreaChart data={chartData} margin={{ top: 5, right: 10, left: -10, bottom: 0 }}>
        <defs>
          <linearGradient id="matchGrad" x1="0" y1="0" x2="0" y2="1">
            <stop offset="5%" stopColor="#6366f1" stopOpacity={0.3} />
            <stop offset="95%" stopColor="#6366f1" stopOpacity={0} />
          </linearGradient>
          <linearGradient id="memberGrad" x1="0" y1="0" x2="0" y2="1">
            <stop offset="5%" stopColor="#22d3ee" stopOpacity={0.25} />
            <stop offset="95%" stopColor="#22d3ee" stopOpacity={0} />
          </linearGradient>
        </defs>
        <CartesianGrid strokeDasharray="3 3" stroke="#374151" opacity={0.3} />
        <XAxis
          dataKey="date"
          tick={{ fill: '#9ca3af', fontSize: 11 }}
          tickLine={false}
          axisLine={{ stroke: '#374151' }}
          interval="preserveStartEnd"
        />
        <YAxis
          tick={{ fill: '#9ca3af', fontSize: 11 }}
          tickLine={false}
          axisLine={{ stroke: '#374151' }}
          width={40}
          allowDecimals={false}
        />
        <Tooltip content={<ActivityTooltip />} cursor={{ stroke: '#6366f1', strokeDasharray: '5 5' }} />
        <Area
          type="monotone"
          dataKey="matchesPlayed"
          stroke="#6366f1"
          strokeWidth={2}
          fill="url(#matchGrad)"
          activeDot={{ r: 4, fill: '#6366f1', stroke: '#1f2937', strokeWidth: 2 }}
          dot={false}
        />
        <Area
          type="monotone"
          dataKey="membersJoined"
          stroke="#22d3ee"
          strokeWidth={2}
          fill="url(#memberGrad)"
          activeDot={{ r: 4, fill: '#22d3ee', stroke: '#1f2937', strokeWidth: 2 }}
          dot={false}
        />
      </AreaChart>
    </ResponsiveContainer>
  );
}

function PlayerRow({ player, rank }: { player: GuildTopPlayerResponse; rank: number }) {
  const medalColors = ['text-yellow-400', 'text-gray-300', 'text-amber-600'];

  return (
    <motion.div variants={staggerItem}>
      <Link
        to={`/profile/${player.userId}`}
        className="flex items-center gap-3 rounded-lg px-3 py-2.5 transition-colors hover:bg-surface-hover group"
      >
        <span
          className={`w-6 text-center text-sm font-bold tabular-nums ${
            rank <= 3 ? medalColors[rank - 1] : 'text-foreground-muted'
          }`}
        >
          {rank}
        </span>
        <UserAvatar username={player.username} avatarUrl={player.avatarUrl} size="sm" />
        <div className="min-w-0 flex-1">
          <p className="truncate text-sm font-medium text-foreground group-hover:text-primary transition-colors">
            {player.username}
          </p>
          <div className="flex items-center gap-3 mt-0.5">
            <span className="text-xs text-foreground-muted">{player.eloPoints} Elo</span>
            <span className="text-xs text-foreground-subtle">{player.totalMatches} matches</span>
          </div>
        </div>
        <div className="w-24 shrink-0">
          <div className="flex items-center justify-between mb-1">
            <span className="text-xs font-medium text-foreground-muted">
              {player.winRate.toFixed(1)}%
            </span>
          </div>
          <ProgressBar
            value={player.winRate}
            variant={player.winRate >= 60 ? 'accent' : player.winRate >= 45 ? 'primary' : 'warning'}
            size="sm"
          />
        </div>
      </Link>
    </motion.div>
  );
}

const periodOptions: { value: Period; label: string }[] = [
  { value: 7, label: '7d' },
  { value: 30, label: '30d' },
  { value: 90, label: '90d' },
];

export function GuildStatsPanel({ guildId }: GuildStatsPanelProps) {
  const [period, setPeriod] = useState<Period>(30);
  const { data: stats, isLoading } = useGuildStats(guildId, undefined, period);

  if (isLoading) {
    return (
      <div className="flex justify-center py-16">
        <Spinner size="lg" />
      </div>
    );
  }

  if (!stats) {
    return (
      <div className="flex flex-col items-center py-16 text-center">
        <span className="text-4xl">📊</span>
        <p className="mt-3 text-sm text-foreground-muted">Stats not available</p>
      </div>
    );
  }

  const winRate = stats.overallWinRate;

  return (
    <div className="space-y-6">
      {/* Period selector */}
      <div className="flex items-center justify-between">
        <h2 className="text-lg font-semibold text-foreground">Guild Statistics</h2>
        <div className="flex rounded-lg border border-border bg-surface p-0.5">
          {periodOptions.map((opt) => (
            <button
              key={opt.value}
              onClick={() => setPeriod(opt.value)}
              className={`rounded-md px-3 py-1 text-xs font-medium transition-all ${
                period === opt.value
                  ? 'bg-primary text-primary-foreground shadow-sm'
                  : 'text-foreground-muted hover:text-foreground'
              }`}
            >
              {opt.label}
            </button>
          ))}
        </div>
      </div>

      {/* Overview cards */}
      <div className="grid grid-cols-2 gap-3 sm:grid-cols-4">
        <StatCard label="Members" value={stats.totalMembers} icon="👥" delay={0} />
        <StatCard label="Matches" value={stats.totalMatches} icon="⚔️" delay={0.06} />
        <StatCard label="Wins" value={stats.totalWins} icon="🏆" delay={0.12} />
        <StatCard label="Win Rate" value={Math.round(winRate)} suffix="%" icon="📈" delay={0.18} />
      </div>

      {/* Activity chart */}
      <motion.div
        initial={{ opacity: 0, y: 12 }}
        animate={{ opacity: 1, y: 0 }}
        transition={{ delay: 0.2 }}
      >
        <Card>
          <CardHeader>
            <CardTitle>Activity Timeline</CardTitle>
            <div className="flex items-center gap-4 text-xs">
              <span className="flex items-center gap-1.5">
                <span className="inline-block h-2 w-2 rounded-full bg-[#6366f1]" />
                Matches
              </span>
              <span className="flex items-center gap-1.5">
                <span className="inline-block h-2 w-2 rounded-full bg-[#22d3ee]" />
                Joined
              </span>
            </div>
          </CardHeader>
          {stats.activityTimeline.length > 0 ? (
            <ActivityChart timeline={stats.activityTimeline} />
          ) : (
            <p className="py-8 text-center text-sm text-foreground-muted">No activity data yet</p>
          )}
        </Card>
      </motion.div>

      {/* Top players */}
      <motion.div
        initial={{ opacity: 0, y: 12 }}
        animate={{ opacity: 1, y: 0 }}
        transition={{ delay: 0.3 }}
      >
        <Card>
          <CardHeader>
            <CardTitle>Top Players</CardTitle>
          </CardHeader>
          {stats.topPlayers.length > 0 ? (
            <motion.div variants={staggerContainer} initial="hidden" animate="show" className="space-y-0.5">
              {stats.topPlayers.map((player, idx) => (
                <PlayerRow key={player.userId} player={player} rank={idx + 1} />
              ))}
            </motion.div>
          ) : (
            <p className="py-8 text-center text-sm text-foreground-muted">No player data yet</p>
          )}
        </Card>
      </motion.div>
    </div>
  );
}
