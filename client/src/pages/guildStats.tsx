import { useParams, Link, useNavigate } from 'react-router-dom';
import { motion } from 'framer-motion';
import { useAuthStore } from '@/stores/authStore';
import { useGuildDetail } from '@/features/guilds/hooks';
import { AnimatedPage, Spinner } from '@/components/ui';
import { GuildStatsPanel } from '@/components/guild/guildStatsPanel';
import { GuildEventCalendar } from '@/components/guild/guildEventCalendar';

export default function GuildStatsPage() {
  const { guildId } = useParams<{ guildId: string }>();
  const navigate = useNavigate();
  const userId = useAuthStore((s) => s.user?.id);
  const { data: guild, isLoading } = useGuildDetail(guildId);

  if (isLoading) {
    return (
      <AnimatedPage>
        <div className="flex justify-center py-20">
          <Spinner size="lg" />
        </div>
      </AnimatedPage>
    );
  }

  if (!guild || !guildId) {
    return (
      <AnimatedPage>
        <div className="flex flex-col items-center py-20 text-center">
          <span className="text-5xl">🔍</span>
          <h3 className="mt-4 text-xl font-bold text-foreground">Guild not found</h3>
          <button
            onClick={() => navigate('/guilds')}
            className="mt-4 text-sm text-primary hover:underline"
          >
            Browse Guilds
          </button>
        </div>
      </AnimatedPage>
    );
  }

  const currentMember = guild.members.find((m) => m.userId === userId);
  const canManage = currentMember?.role === 'Owner' || currentMember?.role === 'Admin';

  return (
    <AnimatedPage>
      <div className="space-y-6">
        {/* Header */}
        <motion.div
          initial={{ opacity: 0, y: -8 }}
          animate={{ opacity: 1, y: 0 }}
          className="flex flex-col gap-2 sm:flex-row sm:items-center sm:justify-between"
        >
          <div className="flex items-center gap-3">
            <Link
              to={`/guilds/${guildId}`}
              className="flex items-center gap-1 rounded-lg px-2 py-1 text-sm text-foreground-muted hover:bg-surface-hover hover:text-foreground transition-colors"
            >
              <svg className="h-4 w-4" fill="none" stroke="currentColor" viewBox="0 0 24 24" strokeWidth={2}>
                <path strokeLinecap="round" strokeLinejoin="round" d="M15.75 19.5 8.25 12l7.5-7.5" />
              </svg>
              Back
            </Link>
            <div>
              <h1 className="text-2xl font-bold text-foreground">{guild.name}</h1>
              <p className="text-sm text-foreground-muted">Analytics & Events</p>
            </div>
          </div>

          {guild.tag && (
            <span className="rounded-lg bg-primary/15 px-3 py-1 text-sm font-bold text-primary self-start">
              [{guild.tag}]
            </span>
          )}
        </motion.div>

        {/* 2-column layout */}
        <div className="grid gap-6 lg:grid-cols-5">
          <div className="lg:col-span-3">
            <GuildStatsPanel guildId={guildId} canManage={canManage} />
          </div>
          <div className="lg:col-span-2">
            <GuildEventCalendar guildId={guildId} canManage={canManage} />
          </div>
        </div>
      </div>
    </AnimatedPage>
  );
}
