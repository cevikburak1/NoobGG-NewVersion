import { Link } from 'react-router-dom';
import { motion } from 'framer-motion';
import { Badge, Button } from '@/components/ui';
import { UserAvatar } from '@/components/common/userAvatar';
import { UpvoteButton } from './upvoteButton';
import type { CommunityPostResponse, CommunityTopicListResponse } from '@/features/community/types';

interface TopicListProps {
  data: CommunityTopicListResponse;
  onPageChange: (page: number) => void;
}

export function TopicList({ data, onPageChange }: TopicListProps) {
  return (
    <div className="space-y-4">
      {data.topics.length === 0 ? (
        <div className="rounded-[28px] border border-border/45 bg-surface/50 p-10 text-center">
          <p className="text-sm font-medium text-foreground">No topics in this board yet</p>
          <p className="mt-2 text-sm text-foreground-muted">
            Use the form above to publish the first thread — no @mention required.
          </p>
        </div>
      ) : (
        data.topics.map((topic, index) => (
          <motion.div
            key={topic.id}
            initial={{ opacity: 0, y: 16 }}
            animate={{ opacity: 1, y: 0 }}
            transition={{ delay: index * 0.04 }}
          >
            <TopicCard topic={topic} />
          </motion.div>
        ))
      )}

      <TopicPagination data={data} onPageChange={onPageChange} />
    </div>
  );
}

function TopicCard({ topic }: { topic: CommunityPostResponse }) {
  return (
    <Link to={`/community/topics/${topic.id}`}>
      <article className="group relative overflow-hidden rounded-[24px] border border-border/50 bg-surface/70 p-5 backdrop-blur-sm transition-all hover:-translate-y-0.5 hover:border-border-hover hover:bg-surface/90">
        <div className="absolute inset-0 bg-[radial-gradient(circle_at_top_right,rgba(124,58,237,0.08),transparent_30%)] opacity-0 transition-opacity group-hover:opacity-100" />
        <div className="relative">
          <div className="flex flex-wrap items-center gap-2">
            {topic.isPinned ? <Badge variant="accent">Pinned</Badge> : null}
            <Badge variant="default">{topic.category}</Badge>
            <span className="text-xs font-medium uppercase tracking-[0.18em] text-foreground-subtle">
              {topic.boardName}
            </span>
          </div>

          <h3
            className="mt-3 text-2xl font-bold tracking-[-0.04em] text-foreground transition-colors group-hover:text-white"
            style={{ fontFamily: "'Bricolage Grotesque', 'Plus Jakarta Sans', var(--font-sans)" }}
          >
            {topic.title}
          </h3>

          <p className="mt-3 line-clamp-3 text-sm leading-7 text-foreground-muted">
            {topic.content}
          </p>

          <div className="mt-5 flex flex-col gap-4 lg:flex-row lg:items-center lg:justify-between">
            <div className="flex items-center gap-3">
              <UserAvatar username={topic.authorUsername} avatarUrl={topic.authorAvatarUrl} size="sm" />
              <div>
                <p className="text-sm font-semibold text-foreground">{topic.authorUsername}</p>
                <p className="text-xs text-foreground-subtle">
                  Active {formatRelativeTime(topic.lastActivityAt)}
                </p>
              </div>
            </div>

            <div className="flex flex-wrap items-center gap-3" onClick={(e) => e.preventDefault()}>
              <UpvoteButton
                targetId={topic.id}
                targetType={0}
                count={topic.upvoteCount}
                hasUpvoted={topic.hasUpvoted}
                size="sm"
              />
              <TopicMetric icon="Replies" value={String(topic.commentCount)} />
              <TopicMetric icon="Opened" value={formatCalendarDate(topic.createdAt)} />
            </div>
          </div>
        </div>
      </article>
    </Link>
  );
}

function TopicMetric({ icon, value }: { icon: string; value: string }) {
  return (
    <div className="rounded-full border border-border/50 px-3 py-1 text-xs text-foreground-muted">
      <span className="font-medium text-foreground">{value}</span> {icon}
    </div>
  );
}

function formatRelativeTime(dateStr: string): string {
  const diff = Date.now() - new Date(dateStr).getTime();
  const mins = Math.floor(diff / 60_000);
  if (mins < 1) return 'just now';
  if (mins < 60) return `${mins}m ago`;
  const hours = Math.floor(mins / 60);
  if (hours < 24) return `${hours}h ago`;
  const days = Math.floor(hours / 24);
  return `${days}d ago`;
}

function formatCalendarDate(dateStr: string): string {
  return new Date(dateStr).toLocaleDateString('en-US', { month: 'short', day: 'numeric' });
}

function TopicPagination({
  data,
  onPageChange,
}: {
  data: CommunityTopicListResponse;
  onPageChange: (page: number) => void;
}) {
  const totalPages = Math.max(1, Math.ceil(data.totalCount / data.pageSize));
  const rangeStart = data.totalCount === 0 ? 0 : (data.page - 1) * data.pageSize + 1;
  const rangeEnd = Math.min(data.page * data.pageSize, data.totalCount);

  const handlePageChange = (page: number) => {
    onPageChange(page);
    window.scrollTo({ top: 0, behavior: 'smooth' });
  };

  return (
    <div className="rounded-2xl border border-border/50 bg-surface/60 p-4">
      <div className="flex flex-col gap-3 sm:flex-row sm:items-center sm:justify-between">
        <div className="text-xs text-foreground-muted">
          Showing <span className="tabular-nums text-foreground">{rangeStart}–{rangeEnd}</span> of{' '}
          <span className="tabular-nums text-foreground">{data.totalCount}</span> topics
          <span className="ml-2 text-foreground-subtle">· Page {data.page} / {totalPages}</span>
        </div>
        <div className="flex flex-wrap items-center gap-1.5">
          <Button variant="ghost" size="sm" disabled={!data.hasPreviousPage} onClick={() => handlePageChange(1)}>
            First
          </Button>
          <Button variant="ghost" size="sm" disabled={!data.hasPreviousPage} onClick={() => handlePageChange(data.page - 1)}>
            Prev
          </Button>
          {getVisiblePages(data.page, totalPages).map((entry, idx) =>
            entry === 'gap' ? (
              <span key={`gap-${idx}`} className="px-1 text-xs text-foreground-subtle" aria-hidden>
                ...
              </span>
            ) : (
              <button
                key={entry}
                type="button"
                onClick={() => handlePageChange(entry)}
                className={`min-h-8 min-w-8 rounded-lg text-xs font-semibold tabular-nums transition-colors ${
                  entry === data.page
                    ? 'bg-primary text-primary-foreground'
                    : 'text-foreground-muted hover:bg-surface-hover hover:text-foreground'
                }`}
              >
                {entry}
              </button>
            ),
          )}
          <Button variant="ghost" size="sm" disabled={!data.hasNextPage} onClick={() => handlePageChange(data.page + 1)}>
            Next
          </Button>
          <Button variant="ghost" size="sm" disabled={!data.hasNextPage} onClick={() => handlePageChange(totalPages)}>
            Last
          </Button>
        </div>
      </div>
    </div>
  );
}

function getVisiblePages(current: number, total: number): (number | 'gap')[] {
  if (total <= 7) return Array.from({ length: total }, (_, i) => i + 1);
  const set = new Set([1, total, current, current - 1, current + 1]);
  const sorted = [...set].filter((p) => p >= 1 && p <= total).sort((a, b) => a - b);
  const out: (number | 'gap')[] = [];
  let prev = 0;
  for (const p of sorted) {
    if (prev && p - prev > 1) out.push('gap');
    out.push(p);
    prev = p;
  }
  return out;
}
