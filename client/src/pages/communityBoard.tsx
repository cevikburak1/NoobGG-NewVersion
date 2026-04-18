import { useMemo, useState } from 'react';
import { Link, useParams } from 'react-router-dom';
import { motion } from 'framer-motion';
import { AnimatedPage, Badge, Spinner } from '@/components/ui';
import { BoardHero } from '@/components/community/boardHero';
import { TopicComposer } from '@/components/community/topicComposer';
import { TopicList } from '@/components/community/topicList';
import { useCommunityBoardsOverview, useCommunityTopics } from '@/features/community/hooks';

const SORT_OPTIONS = [
  { value: 'latest', label: 'Latest' },
  { value: 'mostCommented', label: 'Most commented' },
  { value: 'mostLiked', label: 'Most liked' },
] as const;

export default function CommunityBoardPage() {
  const { boardSlug } = useParams<{ boardSlug: string }>();
  const [page, setPage] = useState(1);
  const [sort, setSort] = useState<(typeof SORT_OPTIONS)[number]['value']>('latest');

  const slug = boardSlug ?? 'general';
  const { data: boardsData, isLoading: boardsLoading } = useCommunityBoardsOverview({ page: 1, pageSize: 200 });
  const { data: topicsData, isLoading: topicsLoading, isError } = useCommunityTopics(slug, sort, page, 10);

  const board = useMemo(
    () => boardsData?.boards.find((item) => item.slug === slug),
    [boardsData, slug],
  );

  if (boardsLoading || topicsLoading) {
    return (
      <div className="flex min-h-[60vh] items-center justify-center">
        <Spinner size="lg" />
      </div>
    );
  }

  if (isError || !board || !topicsData) {
    return (
      <AnimatedPage>
        <div className="rounded-3xl border border-danger/30 bg-danger/5 p-8 text-center">
          <p className="text-danger">Forum board could not be loaded.</p>
        </div>
      </AnimatedPage>
    );
  }

  return (
    <AnimatedPage>
      <div className="space-y-6 pb-8">
        <BoardHero
          board={board}
          eyebrow="Board View"
          stats={[
            { label: 'Topics', value: String(board.topicCount) },
            { label: 'Sort', value: SORT_OPTIONS.find((item) => item.value === sort)?.label ?? 'Latest' },
            { label: 'Access', value: 'Members only' },
          ]}
        />

        <div className="flex flex-wrap items-center justify-between gap-3 rounded-2xl border border-border/50 bg-surface/60 px-4 py-3">
          <div className="flex flex-wrap gap-2">
            {SORT_OPTIONS.map((option) => (
              <button
                key={option.value}
                type="button"
                onClick={() => {
                  setSort(option.value);
                  setPage(1);
                }}
                className={`rounded-full px-3 py-1.5 text-xs font-medium transition-colors ${
                  sort === option.value
                    ? 'bg-primary text-primary-foreground'
                    : 'border border-border/60 bg-surface/70 text-foreground-muted hover:text-foreground'
                }`}
              >
                {option.label}
              </button>
            ))}
          </div>

          <Link to="/community" className="text-xs font-medium uppercase tracking-[0.18em] text-accent/90">
            Back to boards
          </Link>
        </div>

        <div className="grid gap-6 xl:grid-cols-[1.25fr_0.75fr]">
          <div className="space-y-5">
            <TopicComposer boardId={board.id} boardCategory={board.category} boards={boardsData?.boards ?? []} />
            <TopicList data={topicsData} onPageChange={setPage} />
          </div>

          <aside className="space-y-4">
            <motion.div
              initial={{ opacity: 0, y: 14 }}
              animate={{ opacity: 1, y: 0 }}
              className="rounded-[28px] border border-border/50 bg-surface/65 p-5 backdrop-blur-md"
            >
              <p className="text-[11px] font-semibold uppercase tracking-[0.22em] text-accent/90">
                Board Lens
              </p>
              <h3
                className="mt-2 text-2xl font-bold tracking-[-0.04em] text-foreground"
                style={{ fontFamily: "'Bricolage Grotesque', 'Plus Jakarta Sans', var(--font-sans)" }}
              >
                What belongs here
              </h3>
              <p className="mt-3 text-sm leading-7 text-foreground-muted">
                Open a topic when you want more than a single comment: patch reaction, roster call, meta question,
                strategy breakdown, or a debate worth keeping alive.
              </p>

              <div className="mt-5 flex flex-wrap gap-2">
                <Badge variant="accent">Long-form takes</Badge>
                <Badge variant="default">Squad search</Badge>
                <Badge variant="default">Patch debate</Badge>
                <Badge variant="default">Tactics</Badge>
              </div>
            </motion.div>

            {boardsData ? (
              <motion.div
                initial={{ opacity: 0, y: 14 }}
                animate={{ opacity: 1, y: 0 }}
                transition={{ delay: 0.05 }}
                className="rounded-[28px] border border-border/50 bg-surface/65 p-5 backdrop-blur-md"
              >
                <p className="text-[11px] font-semibold uppercase tracking-[0.22em] text-accent/90">
                  Other Boards
                </p>
                <div className="mt-4 space-y-2">
                  {boardsData.boards
                    .filter((item) => item.slug !== slug)
                    .slice(0, 5)
                    .map((item) => (
                      <Link
                        key={item.slug}
                        to={`/community/boards/${item.slug}`}
                        className="block rounded-2xl border border-border/50 px-4 py-3 transition-colors hover:border-border-hover hover:bg-surface-hover"
                      >
                        <div className="flex items-center justify-between gap-3">
                          <div>
                            <p className="font-medium text-foreground">{item.title}</p>
                            <p className="text-xs text-foreground-subtle">{item.topicCount} topics</p>
                          </div>
                          <span className="text-[11px] uppercase tracking-[0.18em] text-accent/90">
                            Open
                          </span>
                        </div>
                      </Link>
                    ))}
                </div>
              </motion.div>
            ) : null}
          </aside>
        </div>
      </div>
    </AnimatedPage>
  );
}
