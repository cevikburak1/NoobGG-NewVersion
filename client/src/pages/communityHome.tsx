import { Link } from 'react-router-dom';
import { motion } from 'framer-motion';
import { AnimatedPage, Badge, Spinner } from '@/components/ui';
import { useCommunityBoardsOverview } from '@/features/community/hooks';
import type { CommunityBoardResponse, CommunityPostResponse } from '@/features/community/types';

export default function CommunityHomePage() {
  const { data, isLoading, isError } = useCommunityBoardsOverview();

  if (isLoading) {
    return (
      <div className="flex min-h-[60vh] items-center justify-center">
        <Spinner size="lg" />
      </div>
    );
  }

  if (isError || !data) {
    return (
      <AnimatedPage>
        <div className="rounded-3xl border border-danger/30 bg-danger/5 p-8 text-center">
          <p className="text-danger">Community forum could not be loaded.</p>
        </div>
      </AnimatedPage>
    );
  }

  return (
    <AnimatedPage>
      <div className="relative space-y-8 pb-8">
        <ForumHero boards={data.boards} />

        <section className="grid gap-4 lg:grid-cols-[1.2fr_0.8fr]">
          <div className="space-y-4">
            <SectionHeader
              eyebrow="Boards"
              title="Choose your arena"
              description="Jump into the global player forum or drop straight into a specific game board."
            />
            <div className="grid gap-4 md:grid-cols-2">
              {data.boards.map((board, index) => (
                <motion.div
                  key={board.slug}
                  initial={{ opacity: 0, y: 16 }}
                  animate={{ opacity: 1, y: 0 }}
                  transition={{ delay: index * 0.05 }}
                >
                  <BoardCard board={board} />
                </motion.div>
              ))}
            </div>
          </div>

          <div className="space-y-4">
            <SectionHeader
              eyebrow="Trending"
              title="What everyone is reacting to"
              description="Fastest-moving debates across the forum right now."
            />
            <TopicStack topics={data.trendingTopics} tone="accent" />
          </div>
        </section>

        <section className="space-y-4">
          <SectionHeader
            eyebrow="Latest"
            title="Fresh drops from the boards"
            description="Newest threads across general and game-specific communities."
          />
          {data.latestTopics.length === 0 ? (
            <div className="rounded-2xl border border-dashed border-border/60 bg-surface/50 p-8 text-center text-sm text-foreground-muted">
              No topics yet. Be the first to open a thread.
            </div>
          ) : (
            <div className="grid gap-4 lg:grid-cols-3">
              {data.latestTopics.map((topic, index) => (
                <motion.div
                  key={topic.id}
                  initial={{ opacity: 0, y: 18 }}
                  animate={{ opacity: 1, y: 0 }}
                  transition={{ delay: 0.05 * index }}
                >
                  <LatestTopicCard topic={topic} />
                </motion.div>
              ))}
            </div>
          )}
        </section>
      </div>
    </AnimatedPage>
  );
}

function ForumHero({ boards }: { boards: CommunityBoardResponse[] }) {
  const totalTopics = boards.reduce((sum, board) => sum + board.topicCount, 0);

  return (
    <section className="relative overflow-hidden rounded-[32px] border border-border/50 bg-surface/70 px-6 py-8 sm:px-8 sm:py-10">
      <div className="absolute inset-0 bg-[radial-gradient(circle_at_top_left,rgba(6,214,160,0.12),transparent_25%),radial-gradient(circle_at_top_right,rgba(124,58,237,0.18),transparent_30%)]" />
      <div className="absolute inset-0 bg-[linear-gradient(135deg,transparent_0%,rgba(255,255,255,0.02)_100%)]" />
      <div className="relative">
        <div className="max-w-4xl">
          <p className="text-[11px] font-semibold uppercase tracking-[0.24em] text-accent/90">
            NoobGg Community
          </p>
          <h1
            className="mt-3 text-5xl font-bold tracking-[-0.06em] text-foreground sm:text-6xl lg:text-7xl"
            style={{ fontFamily: "'Bricolage Grotesque', 'Plus Jakarta Sans', var(--font-sans)" }}
          >
            The strategy room, the callout feed, the squad board.
          </h1>
          <p className="mt-5 max-w-2xl text-sm leading-7 text-foreground-muted sm:text-base">
            Separate from game detail comments, this is the full forum layer for players: long-form takes, roster asks,
            patch reactions, board-specific threads, and a home that feels alive.
          </p>
        </div>

        <div className="mt-8 flex flex-wrap gap-3">
          <HeroStat label="Boards" value={String(boards.length)} />
          <HeroStat label="Active topics" value={String(totalTopics)} />
          <HeroStat label="Mode" value="Auth-only" />
        </div>
      </div>
    </section>
  );
}

function HeroStat({ label, value }: { label: string; value: string }) {
  return (
    <div className="rounded-2xl border border-white/10 bg-black/20 px-4 py-3 backdrop-blur-md">
      <p className="text-[11px] uppercase tracking-[0.22em] text-foreground-subtle">{label}</p>
      <p className="mt-1 text-2xl font-semibold text-foreground">{value}</p>
    </div>
  );
}

function SectionHeader({
  eyebrow,
  title,
  description,
}: {
  eyebrow: string;
  title: string;
  description: string;
}) {
  return (
    <div>
      <p className="text-[11px] font-semibold uppercase tracking-[0.24em] text-accent/90">{eyebrow}</p>
      <h2
        className="mt-2 text-3xl font-bold tracking-[-0.05em] text-foreground"
        style={{ fontFamily: "'Bricolage Grotesque', 'Plus Jakarta Sans', var(--font-sans)" }}
      >
        {title}
      </h2>
      <p className="mt-2 text-sm leading-7 text-foreground-muted">{description}</p>
    </div>
  );
}

function BoardCard({ board }: { board: CommunityBoardResponse }) {
  return (
    <Link to={`/community/boards/${board.slug}`} className="block h-full">
      <article className="group relative h-full overflow-hidden rounded-[28px] border border-border/50 bg-surface/75 p-5 backdrop-blur-md transition-all hover:-translate-y-0.5 hover:border-border-hover">
        {board.coverImageUrl ? (
          <div
            className="absolute inset-0 bg-cover bg-center opacity-20 transition-opacity group-hover:opacity-30"
            style={{ backgroundImage: `url(${board.coverImageUrl})` }}
          />
        ) : null}
        <div className={`absolute inset-0 bg-linear-to-br ${board.accent}`} />
        <div className="absolute inset-0 bg-linear-to-t from-background via-background/70 to-background/20" />

        <div className="relative flex h-full flex-col">
          <div className="flex items-center justify-between gap-3">
            <Badge variant={board.boardType === 'General' ? 'accent' : 'default'}>
              {board.boardType === 'General' ? 'General' : 'Game'}
            </Badge>
            <span className="text-xs text-foreground-subtle">
              {board.topicCount} topics
            </span>
          </div>
          <h3
            className="mt-5 text-3xl font-bold tracking-[-0.05em] text-foreground"
            style={{ fontFamily: "'Bricolage Grotesque', 'Plus Jakarta Sans', var(--font-sans)" }}
          >
            {board.title}
          </h3>
          <p className="mt-3 flex-1 text-sm leading-7 text-foreground-muted">
            {board.description}
          </p>
          <div className="mt-6 flex items-center justify-between">
            <span className="text-xs uppercase tracking-[0.18em] text-accent/90">
              Enter board
            </span>
            <span className="text-xs text-foreground-subtle">
              {formatRelativeTime(board.lastActivityAt)}
            </span>
          </div>
        </div>
      </article>
    </Link>
  );
}

function TopicStack({ topics, tone }: { topics: CommunityPostResponse[]; tone: 'accent' | 'neutral' }) {
  if (topics.length === 0) {
    return (
      <div className="rounded-2xl border border-dashed border-border/60 bg-surface/50 p-8 text-center text-sm text-foreground-muted">
        No trending topics yet. Start a thread and get the conversation going.
      </div>
    );
  }

  return (
    <div className="space-y-3">
      {topics.map((topic) => (
        <Link key={topic.id} to={`/community/topics/${topic.id}`}>
          <article className={`rounded-[24px] border p-4 transition-colors hover:border-border-hover ${
            tone === 'accent'
              ? 'border-primary/20 bg-primary/8'
              : 'border-border/50 bg-surface/70'
          }`}>
            <div className="flex items-start justify-between gap-3">
              <div className="min-w-0 flex-1">
                <div className="flex flex-wrap items-center gap-2">
                  <span className="text-[11px] font-medium uppercase tracking-[0.18em] text-foreground-subtle">
                    {topic.gameName ?? 'General'}
                  </span>
                  <Badge variant="default">{topic.category}</Badge>
                </div>
                <h3 className="mt-2 text-lg font-semibold text-foreground">{topic.title}</h3>
                <p className="mt-1.5 line-clamp-2 text-sm leading-6 text-foreground-muted">{topic.content}</p>
              </div>
            </div>
            <div className="mt-3 flex flex-wrap items-center gap-3 text-xs text-foreground-subtle">
              <span className="font-medium text-foreground">{topic.authorUsername}</span>
              <span>{topic.commentCount} replies</span>
              <span>{topic.upvoteCount} upvotes</span>
              <span>{formatRelativeTime(topic.lastActivityAt)}</span>
            </div>
          </article>
        </Link>
      ))}
    </div>
  );
}

function LatestTopicCard({ topic }: { topic: CommunityPostResponse }) {
  return (
    <Link to={`/community/topics/${topic.id}`} className="block h-full">
      <article className="group h-full rounded-[26px] border border-border/50 bg-surface/70 p-5 transition-all hover:-translate-y-0.5 hover:border-border-hover hover:bg-surface/85">
        <div className="flex items-center gap-2">
          <Badge variant="default">{topic.category}</Badge>
          <span className="text-[11px] font-medium uppercase tracking-[0.18em] text-foreground-subtle">
            {topic.gameName ?? 'General'}
          </span>
        </div>
        <h3 className="mt-4 text-xl font-bold tracking-[-0.03em] text-foreground transition-colors group-hover:text-white">
          {topic.title}
        </h3>
        <p className="mt-2 line-clamp-3 text-sm leading-7 text-foreground-muted">{topic.content}</p>
        <div className="mt-4 flex items-center justify-between border-t border-border/30 pt-3">
          <div className="flex items-center gap-2">
            <div className="flex h-6 w-6 items-center justify-center rounded-full bg-primary/20 text-[10px] font-semibold text-primary">
              {topic.authorUsername.slice(0, 2).toUpperCase()}
            </div>
            <span className="text-xs font-medium text-foreground">{topic.authorUsername}</span>
          </div>
          <div className="flex items-center gap-3 text-[11px] text-foreground-subtle">
            <span>{topic.commentCount} replies</span>
            <span>{formatRelativeTime(topic.lastActivityAt)}</span>
          </div>
        </div>
      </article>
    </Link>
  );
}

function formatRelativeTime(dateStr: string | null) {
  if (!dateStr) return 'No activity yet';
  const diff = Date.now() - new Date(dateStr).getTime();
  const mins = Math.floor(diff / 60_000);
  if (mins < 1) return 'just now';
  if (mins < 60) return `${mins}m ago`;
  const hours = Math.floor(mins / 60);
  if (hours < 24) return `${hours}h ago`;
  const days = Math.floor(hours / 24);
  return `${days}d ago`;
}
