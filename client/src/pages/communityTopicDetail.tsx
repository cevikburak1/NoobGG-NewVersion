import { Link, useParams } from 'react-router-dom';
import { motion } from 'framer-motion';
import { AnimatedPage, Badge, Spinner } from '@/components/ui';
import { TopicDetail } from '@/components/community/topicDetail';
import { TopicReplies } from '@/components/community/topicReplies';
import { useCommunityTopicDetail } from '@/features/community/hooks';

export default function CommunityTopicDetailPage() {
  const { topicId } = useParams<{ topicId: string }>();
  const { data, isLoading, isError } = useCommunityTopicDetail(topicId);

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
          <p className="text-danger">Topic could not be loaded.</p>
        </div>
      </AnimatedPage>
    );
  }

  const boardLink = `/community/boards/${data.topic.boardSlug}`;

  return (
    <AnimatedPage>
      <div className="space-y-6 pb-8">
        <div className="flex flex-wrap items-center gap-3">
          <Link
            to={boardLink}
            className="text-[11px] font-semibold uppercase tracking-[0.22em] text-accent/90"
          >
            Back to board
          </Link>
          <Badge variant="default">{data.topic.category}</Badge>
          {data.topic.gameName ? <Badge variant="accent">{data.topic.gameName}</Badge> : null}
        </div>

        <div className="grid gap-6 xl:grid-cols-[1.25fr_0.75fr]">
          <div className="space-y-6">
            <TopicDetail topic={data.topic} />
            <TopicReplies topicId={data.topic.id} isLocked={data.topic.isLocked} />
          </div>

          <aside className="space-y-4">
            <motion.div
              initial={{ opacity: 0, y: 16 }}
              animate={{ opacity: 1, y: 0 }}
              className="rounded-[28px] border border-border/50 bg-surface/65 p-5 backdrop-blur-md"
            >
              <p className="text-[11px] font-semibold uppercase tracking-[0.22em] text-accent/90">
                Thread Snapshot
              </p>
              <div className="mt-4 grid gap-3">
                <SnapshotRow label="Replies" value={String(data.topic.commentCount)} />
                <SnapshotRow label="Upvotes" value={String(data.topic.upvoteCount)} />
                <SnapshotRow label="Board" value={data.topic.boardName} />
              </div>
            </motion.div>

            <motion.div
              initial={{ opacity: 0, y: 16 }}
              animate={{ opacity: 1, y: 0 }}
              transition={{ delay: 0.05 }}
              className="rounded-[28px] border border-border/50 bg-surface/65 p-5 backdrop-blur-md"
            >
              <p className="text-[11px] font-semibold uppercase tracking-[0.22em] text-accent/90">
                Related Threads
              </p>
              <div className="mt-4 space-y-3">
                {data.relatedTopics.length > 0 ? (
                  data.relatedTopics.map((topic) => (
                    <Link
                      key={topic.id}
                      to={`/community/topics/${topic.id}`}
                      className="block rounded-2xl border border-border/50 px-4 py-3 transition-colors hover:border-border-hover hover:bg-surface-hover"
                    >
                      <p className="text-sm font-semibold text-foreground">{topic.title}</p>
                      <p className="mt-1 text-xs leading-6 text-foreground-subtle">
                        {topic.commentCount} replies · {topic.upvoteCount} upvotes
                      </p>
                    </Link>
                  ))
                ) : (
                  <p className="text-sm text-foreground-muted">No related threads yet.</p>
                )}
              </div>
            </motion.div>
          </aside>
        </div>
      </div>
    </AnimatedPage>
  );
}

function SnapshotRow({ label, value }: { label: string; value: string }) {
  return (
    <div className="rounded-2xl border border-border/50 bg-background/25 px-4 py-3">
      <p className="text-[11px] uppercase tracking-[0.22em] text-foreground-subtle">{label}</p>
      <p className="mt-1 text-xl font-semibold text-foreground">{value}</p>
    </div>
  );
}
