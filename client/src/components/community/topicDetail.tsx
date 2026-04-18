import { Link } from 'react-router-dom';
import { motion } from 'framer-motion';
import { Badge } from '@/components/ui';
import { UserAvatar } from '@/components/common/userAvatar';
import { UpvoteButton } from './upvoteButton';
import type { CommunityPostResponse } from '@/features/community/types';

export function TopicDetail({ topic }: { topic: CommunityPostResponse }) {
  return (
    <motion.article
      initial={{ opacity: 0, y: 18 }}
      animate={{ opacity: 1, y: 0 }}
      className="overflow-hidden rounded-[28px] border border-border/50 bg-surface/75 p-6 backdrop-blur-md sm:p-7"
    >
      <div className="flex flex-wrap items-center gap-2">
        <Link
          to={`/community/boards/${topic.boardSlug}`}
          className="text-[11px] font-semibold uppercase tracking-[0.22em] text-accent/90"
        >
          {topic.boardName}
        </Link>
        <Badge variant="default">{topic.category}</Badge>
        {topic.isPinned ? <Badge variant="accent">Pinned</Badge> : null}
      </div>

      <h1
        className="mt-4 text-4xl font-bold tracking-[-0.05em] text-foreground sm:text-5xl"
        style={{ fontFamily: "'Bricolage Grotesque', 'Plus Jakarta Sans', var(--font-sans)" }}
      >
        {topic.title}
      </h1>

      <div className="mt-6 flex flex-col gap-4 border-y border-border/40 py-5 sm:flex-row sm:items-center sm:justify-between">
        <div className="flex items-center gap-3">
          <UserAvatar username={topic.authorUsername} avatarUrl={topic.authorAvatarUrl} size="md" />
          <div>
            <p className="font-semibold text-foreground">{topic.authorUsername}</p>
            <p className="text-sm text-foreground-subtle">
              Opened {formatRelativeTime(topic.createdAt)} · Active {formatRelativeTime(topic.lastActivityAt)}
            </p>
          </div>
        </div>
        <UpvoteButton
          targetId={topic.id}
          targetType={0}
          count={topic.upvoteCount}
          hasUpvoted={topic.hasUpvoted}
        />
      </div>

      {topic.imageUrl ? (
        <img src={topic.imageUrl} alt="" className="mt-6 max-h-[420px] w-full rounded-[24px] object-cover" />
      ) : null}

      <div className="prose prose-invert mt-6 max-w-none">
        <p className="whitespace-pre-wrap text-base leading-8 text-foreground-muted">
          {topic.content}
        </p>
      </div>
    </motion.article>
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
