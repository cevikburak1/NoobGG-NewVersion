import { useState } from 'react';
import { motion } from 'framer-motion';
import { Button, Textarea } from '@/components/ui';
import { UserAvatar } from '@/components/common/userAvatar';
import { useAddTopicComment, useTopicComments } from '@/features/community/hooks';
import { UpvoteButton } from './upvoteButton';

export function TopicReplies({ topicId, isLocked = false }: { topicId: string; isLocked?: boolean }) {
  const [page, setPage] = useState(1);
  const [reply, setReply] = useState('');
  const { data, isLoading } = useTopicComments(topicId, page, 10);
  const addComment = useAddTopicComment();

  const canSubmit = reply.trim().length > 0 && !isLocked;

  const handleSubmit = () => {
    if (!canSubmit) return;

    addComment.mutate(
      { postId: topicId, content: reply.trim() },
      {
        onSuccess: () => setReply(''),
      },
    );
  };

  return (
    <section className="space-y-4">
      <div className="rounded-[28px] border border-border/50 bg-surface/70 p-5 backdrop-blur-md sm:p-6">
        <div className="flex items-center justify-between gap-3">
          <div>
            <p className="text-[11px] font-semibold uppercase tracking-[0.22em] text-accent/90">
              Replies
            </p>
            <h2
              className="mt-2 text-2xl font-bold tracking-[-0.04em] text-foreground"
              style={{ fontFamily: "'Bricolage Grotesque', 'Plus Jakarta Sans', var(--font-sans)" }}
            >
              Keep the thread moving
            </h2>
          </div>
          <span className="rounded-full border border-border/50 px-3 py-1 text-xs text-foreground-muted">
            {data?.totalCount ?? 0} replies
          </span>
        </div>

        <div className="mt-4 space-y-3">
          <Textarea
            value={reply}
            onChange={(event) => setReply(event.target.value)}
            rows={4}
            disabled={isLocked}
            placeholder={isLocked ? 'This topic is locked.' : 'Drop your reply, counterpoint, or squad callout.'}
            className="border-border/60 bg-background/60"
          />
          <div className="flex items-center justify-between gap-3">
            <p className="text-xs text-foreground-subtle">
              {isLocked ? 'Locked topics cannot receive new replies.' : 'Replies stay flat in v1 for speed and clarity.'}
            </p>
            <Button
              onClick={handleSubmit}
              disabled={!canSubmit}
              isLoading={addComment.isPending}
            >
              Post Reply
            </Button>
          </div>
        </div>
      </div>

      <div className="space-y-3">
        {isLoading ? (
          <div className="rounded-2xl border border-border/50 bg-surface/60 p-6 text-sm text-foreground-muted">
            Loading replies...
          </div>
        ) : data && data.comments.length > 0 ? (
          data.comments.map((comment, index) => (
            <motion.article
              key={comment.id}
              initial={{ opacity: 0, x: -10 }}
              animate={{ opacity: 1, x: 0 }}
              transition={{ delay: index * 0.04 }}
              className="rounded-[24px] border border-border/50 bg-surface/65 p-5 backdrop-blur-sm"
            >
              <div className="flex gap-3">
                <UserAvatar username={comment.authorUsername} avatarUrl={comment.authorAvatarUrl} size="sm" />
                <div className="min-w-0 flex-1">
                  <div className="flex flex-wrap items-center gap-x-3 gap-y-1">
                    <span className="font-semibold text-foreground">{comment.authorUsername}</span>
                    <span className="text-xs text-foreground-subtle">{formatRelativeTime(comment.createdAt)}</span>
                  </div>
                  <p className="mt-2 whitespace-pre-wrap text-sm leading-7 text-foreground-muted">
                    {comment.content}
                  </p>
                  <div className="mt-3">
                    <UpvoteButton
                      targetId={comment.id}
                      targetType={2}
                      count={comment.upvoteCount}
                      hasUpvoted={comment.hasUpvoted}
                      size="sm"
                    />
                  </div>
                </div>
              </div>
            </motion.article>
          ))
        ) : (
          <div className="rounded-2xl border border-dashed border-border/60 bg-surface/50 p-8 text-center text-sm text-foreground-muted">
            No replies yet. Start the first one.
          </div>
        )}
      </div>

      {data && (data.page > 1 || data.hasMore) ? (
        <div className="flex items-center justify-between rounded-2xl border border-border/50 bg-surface/60 p-4">
          <Button variant="ghost" size="sm" disabled={page === 1} onClick={() => setPage((value) => value - 1)}>
            Previous replies
          </Button>
          <span className="text-xs text-foreground-muted">
            Page {data.page}
          </span>
          <Button variant="outline" size="sm" disabled={!data.hasMore} onClick={() => setPage((value) => value + 1)}>
            More replies
          </Button>
        </div>
      ) : null}
    </section>
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
