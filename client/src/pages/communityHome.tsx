import { useMemo, useState } from 'react';
import { Link } from 'react-router-dom';
import { motion } from 'framer-motion';
import { AnimatedPage, Badge, Button, Input, Modal, Select, Spinner, Textarea } from '@/components/ui';
import { useToast } from '@/components/ui/toast';
import { useCommunityBoardsOverview, useCreateBoard } from '@/features/community/hooks';
import { useGameBrowse } from '@/features/games/hooks';
import type {
  CommunityBoardResponse,
  CommunityPostResponse,
  CreateBoardPayload,
} from '@/features/community/types';

export default function CommunityHomePage() {
  const [category, setCategory] = useState<string>('all');
  const [query, setQuery] = useState('');
  const [isCreateBoardOpen, setIsCreateBoardOpen] = useState(false);

  const params = useMemo(
    () => ({
      category: category === 'all' ? undefined : category,
      q: query.trim() || undefined,
      sort: 'activity' as const,
      page: 1,
      pageSize: 40,
    }),
    [category, query],
  );
  const { data, isLoading, isError } = useCommunityBoardsOverview(params);

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

        <section className="rounded-[24px] border border-border/50 bg-surface/65 p-4 backdrop-blur-sm">
          <div className="flex flex-col gap-3 lg:flex-row lg:items-center lg:justify-between">
            <div className="flex-1">
              <p className="text-[11px] font-semibold uppercase tracking-[0.22em] text-accent/90">Discover</p>
              <div className="mt-2 flex flex-wrap gap-2">
                <button
                  type="button"
                  onClick={() => setCategory('all')}
                  className={`rounded-full px-3 py-1.5 text-xs font-medium transition-colors ${
                    category === 'all'
                      ? 'bg-primary text-primary-foreground'
                      : 'border border-border/60 bg-surface/70 text-foreground-muted hover:text-foreground'
                  }`}
                >
                  All categories
                </button>
                {data.boardCategories.map((item) => (
                  <button
                    key={item}
                    type="button"
                    onClick={() => setCategory(item)}
                    className={`rounded-full px-3 py-1.5 text-xs font-medium transition-colors ${
                      category === item
                        ? 'bg-primary text-primary-foreground'
                        : 'border border-border/60 bg-surface/70 text-foreground-muted hover:text-foreground'
                    }`}
                  >
                    {item}
                  </button>
                ))}
              </div>
            </div>
            <div className="flex w-full flex-col gap-2 sm:w-auto sm:flex-row">
              <Input
                value={query}
                onChange={(event) => setQuery(event.target.value)}
                placeholder="Search board name or description"
                className="min-w-64 border-border/60 bg-background/60"
              />
              <Button onClick={() => setIsCreateBoardOpen(true)} className="sm:min-w-40">
                Create Board
              </Button>
            </div>
          </div>
        </section>

        <section className="grid gap-4 lg:grid-cols-[1.2fr_0.8fr]">
          <div className="space-y-4">
            <SectionHeader
              eyebrow="Boards"
              title="Choose your arena"
              description="Open boards, browse categories, and jump into conversations by topic."
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
              description="Fastest-moving debates ranked by reply velocity."
            />
            <TopicStack topics={data.trendingTopics} tone="accent" emptyLabel="Nothing trending right now" />
          </div>
        </section>

        <section className="grid gap-4 lg:grid-cols-2">
          <div className="space-y-4">
            <SectionHeader
              eyebrow="Top Discussed"
              title="Most commented threads"
              description="Threads with the highest reply counts right now."
            />
            <TopicStack topics={data.topDiscussedTopics} tone="neutral" emptyLabel="No reply-heavy threads yet" />
          </div>
          <div className="space-y-4">
            <SectionHeader
              eyebrow="Top Liked"
              title="Most liked threads"
              description="Conversations the community is upvoting the most."
            />
            <TopicStack topics={data.mostLikedTopics} tone="neutral" emptyLabel="No upvoted threads yet" />
          </div>
        </section>

        <section className="space-y-4">
          <SectionHeader
            eyebrow="Latest"
            title="Fresh drops from the boards"
            description="Newest threads across general and game-specific communities."
          />
          {data.latestTopics.length === 0 ? (
            <LatestTopicsEmpty firstBoardSlug={data.boards[0]?.slug} />
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
      <CreateBoardModal isOpen={isCreateBoardOpen} onClose={() => setIsCreateBoardOpen(false)} />
    </AnimatedPage>
  );
}

function ForumHero({ boards }: { boards: CommunityBoardResponse[] }) {
  const totalTopics = boards.reduce((sum, board) => sum + board.topicCount, 0);

  return (
    <section className="relative overflow-hidden rounded-[32px] border border-border/50 bg-surface/70 px-6 py-8 sm:px-8 sm:py-10">
      <div
        className="absolute inset-0 bg-cover bg-center opacity-[0.42]"
        style={{ backgroundImage: "url('/images/community-hero-bg.svg')" }}
        aria-hidden
      />
      <div className="absolute inset-0 bg-linear-to-b from-background/88 via-background/78 to-background/92" />
      <div className="absolute inset-0 bg-[radial-gradient(circle_at_top_left,rgba(6,214,160,0.1),transparent_28%),radial-gradient(circle_at_top_right,rgba(124,58,237,0.14),transparent_32%)]" />
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
            <Badge variant="accent">{board.category}</Badge>
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

function TopicStack({
  topics,
  tone,
  emptyLabel = 'No threads yet',
}: {
  topics: CommunityPostResponse[];
  tone: 'accent' | 'neutral';
  emptyLabel?: string;
}) {
  if (topics.length === 0) {
    return (
      <div className="rounded-2xl border border-border/35 bg-surface/35 px-4 py-5 text-center">
        <p className="text-xs text-foreground-subtle">{emptyLabel}</p>
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
                    {topic.boardName}
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

function LatestTopicsEmpty({ firstBoardSlug }: { firstBoardSlug?: string }) {
  return (
    <div className="relative overflow-hidden rounded-[28px] border border-border/45 bg-surface/55 p-8 sm:p-10">
      <div className="pointer-events-none absolute -right-8 -top-8 h-40 w-40 rounded-full bg-primary/15 blur-3xl" />
      <div className="relative mx-auto max-w-md text-center">
        <div className="mx-auto mb-4 flex h-12 w-12 items-center justify-center rounded-2xl border border-border/50 bg-background/40">
          <svg className="h-6 w-6 text-primary" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="1.5" aria-hidden>
            <path strokeLinecap="round" strokeLinejoin="round" d="M8 12h.01M12 12h.01M16 12h.01M21 12c0 4.418-4.03 8-9 8a9.863 9.863 0 01-4.255-.949L3 20l1.395-3.72C3.512 15.042 3 13.574 3 12c0-4.418 4.03-8 9-8s9 3.582 9 8z" />
          </svg>
        </div>
        <h3 className="text-lg font-semibold text-foreground">Nothing in Latest yet</h3>
        <p className="mt-2 text-sm leading-relaxed text-foreground-muted">
          Open a board and publish a topic — it will show up here for everyone.
        </p>
        {firstBoardSlug ? (
          <Link
            to={`/community/boards/${firstBoardSlug}`}
            className="mt-5 inline-flex items-center justify-center rounded-full bg-primary px-5 py-2.5 text-sm font-semibold text-primary-foreground transition-opacity hover:opacity-90"
          >
            Go to a board
          </Link>
        ) : null}
      </div>
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
            {topic.boardName}
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

function CreateBoardModal({ isOpen, onClose }: { isOpen: boolean; onClose: () => void }) {
  const { addToast } = useToast();
  const createBoard = useCreateBoard();
  const { data: gamesPage } = useGameBrowse({ page: 1, pageSize: 80 });
  const [name, setName] = useState('');
  const [description, setDescription] = useState('');
  const [category, setCategory] = useState('General');
  const [gameId, setGameId] = useState('');
  const [coverImageUrl, setCoverImageUrl] = useState('');

  const slugPreview = useMemo(() => buildSlug(name), [name]);
  const gameOptions = useMemo(
    () => (gamesPage?.items ?? []).map((g) => ({ value: g.id, label: g.name })),
    [gamesPage],
  );
  const canSubmit = name.trim().length >= 3 && description.trim().length >= 20 && category.trim().length > 0;

  const handleSubmit = () => {
    if (!canSubmit) return;
    const payload: CreateBoardPayload = {
      name: name.trim(),
      description: description.trim(),
      category: category.trim(),
      slug: slugPreview || undefined,
      gameId: gameId || undefined,
      coverImageUrl: coverImageUrl.trim() || undefined,
    };
    createBoard.mutate(payload, {
      onSuccess: () => {
        addToast({ title: 'Board created', message: 'Your board is now live.', type: 'success' });
        setName('');
        setDescription('');
        setCategory('General');
        setGameId('');
        setCoverImageUrl('');
        onClose();
      },
      onError: () => {
        addToast({ title: 'Could not create board', message: 'Try another board name or slug.', type: 'error' });
      },
    });
  };

  return (
    <Modal isOpen={isOpen} onClose={onClose} title="Create Board" className="max-w-lg">
      <div className="space-y-3">
        <Input value={name} onChange={(event) => setName(event.target.value)} placeholder="Board name" />
        <Input value={category} onChange={(event) => setCategory(event.target.value)} placeholder="Category (e.g. Strategy)" />
        <Textarea
          value={description}
          onChange={(event) => setDescription(event.target.value)}
          rows={4}
          placeholder="Describe what belongs in this board."
        />
        {gameOptions.length > 0 ? (
          <Select
            label="Linked game (optional)"
            placeholder="No game link"
            value={gameId}
            onChange={(e) => setGameId(e.target.value)}
            options={gameOptions}
          />
        ) : null}
        <Input
          value={coverImageUrl}
          onChange={(event) => setCoverImageUrl(event.target.value)}
          placeholder="Cover image URL (optional)"
        />
        <p className="text-xs text-foreground-subtle">
          Slug preview: <span className="text-foreground">{slugPreview || '(type a board name)'}</span>
        </p>
        <div className="flex justify-end gap-2">
          <Button variant="ghost" onClick={onClose}>Cancel</Button>
          <Button onClick={handleSubmit} isLoading={createBoard.isPending} disabled={!canSubmit}>
            Create
          </Button>
        </div>
      </div>
    </Modal>
  );
}

function buildSlug(value: string) {
  const slug = value
    .trim()
    .toLowerCase()
    .replace(/[^a-z0-9]+/g, '-')
    .replace(/^-+|-+$/g, '');
  return slug.slice(0, 80);
}
