import { motion } from 'framer-motion';
import { Badge } from '@/components/ui';
import type { CommunityBoardResponse } from '@/features/community/types';

interface BoardHeroProps {
  board: CommunityBoardResponse;
  eyebrow?: string;
  stats?: Array<{ label: string; value: string }>;
}

export function BoardHero({ board, eyebrow = 'Forum Board', stats = [] }: BoardHeroProps) {
  return (
    <motion.section
      initial={{ opacity: 0, y: 18 }}
      animate={{ opacity: 1, y: 0 }}
      transition={{ duration: 0.45 }}
      className="relative overflow-hidden rounded-[28px] border border-border/50 bg-surface/70"
    >
      {board.coverImageUrl ? (
        <div
          className="absolute inset-0 bg-cover bg-center opacity-30"
          style={{ backgroundImage: `url(${board.coverImageUrl})` }}
        />
      ) : null}
      <div className="absolute inset-0 bg-[radial-gradient(circle_at_top_left,rgba(255,255,255,0.08),transparent_35%)]" />
      <div className={`absolute inset-0 bg-linear-to-br ${board.accent}`} />
      <div className="absolute inset-0 bg-linear-to-t from-background via-background/65 to-background/15" />

      <div className="relative space-y-8 px-6 py-7 sm:px-8 sm:py-9">
        <div className="flex flex-col gap-3 lg:flex-row lg:items-end lg:justify-between">
          <div className="max-w-3xl">
            <p className="text-[11px] font-semibold uppercase tracking-[0.24em] text-accent/90">
              {eyebrow}
            </p>
            <h1
              className="mt-2 text-4xl font-bold tracking-[-0.04em] text-foreground sm:text-5xl"
              style={{ fontFamily: "'Bricolage Grotesque', 'Plus Jakarta Sans', var(--font-sans)" }}
            >
              {board.title}
            </h1>
            <p className="mt-3 max-w-2xl text-sm leading-7 text-foreground-muted sm:text-base">
              {board.description}
            </p>
          </div>

          <div className="flex flex-wrap gap-2">
            <Badge variant="accent" className="px-3 py-1.5 text-[11px] uppercase tracking-[0.2em]">
              {board.category}
            </Badge>
            <Badge variant="default" className="px-3 py-1.5 text-xs">
              {board.topicCount} topics
            </Badge>
          </div>
        </div>

        {stats.length > 0 ? (
          <div className="grid gap-3 sm:grid-cols-3">
            {stats.map((stat, index) => (
              <motion.div
                key={stat.label}
                initial={{ opacity: 0, y: 12 }}
                animate={{ opacity: 1, y: 0 }}
                transition={{ delay: 0.08 * index }}
                className="rounded-2xl border border-white/10 bg-black/20 px-4 py-4 backdrop-blur-md"
              >
                <p className="text-[11px] uppercase tracking-[0.2em] text-foreground-subtle">{stat.label}</p>
                <p className="mt-2 text-2xl font-semibold text-foreground">{stat.value}</p>
              </motion.div>
            ))}
          </div>
        ) : null}
      </div>
    </motion.section>
  );
}
