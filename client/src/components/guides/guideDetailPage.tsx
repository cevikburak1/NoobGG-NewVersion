import { useParams, useNavigate } from 'react-router-dom';
import { motion } from 'framer-motion';
import { AnimatedPage, Badge, Spinner } from '@/components/ui';
import { UserAvatar } from '@/components/common/userAvatar';
import { UpvoteButton } from '@/components/community/upvoteButton';
import { useGuideDetail } from '@/features/guides/hooks';

function formatDate(dateStr: string): string {
  return new Date(dateStr).toLocaleDateString('en-US', {
    year: 'numeric',
    month: 'long',
    day: 'numeric',
  });
}

export default function GuideDetailPage() {
  const { guideId } = useParams<{ guideId: string }>();
  const navigate = useNavigate();
  const { data: guide, isLoading, error } = useGuideDetail(guideId);

  if (isLoading) {
    return (
      <div className="flex min-h-[60vh] items-center justify-center">
        <Spinner size="lg" />
      </div>
    );
  }

  if (error || !guide) {
    return (
      <AnimatedPage>
        <div className="flex flex-col items-center py-32 text-center">
          <motion.div
            initial={{ scale: 0 }}
            animate={{ scale: 1 }}
            transition={{ type: 'spring', bounce: 0.5 }}
            className="text-7xl"
          >
            📖
          </motion.div>
          <h2 className="mt-6 text-2xl font-bold text-foreground">Guide not found</h2>
          <p className="mt-2 text-foreground-muted">This guide may have been removed or doesn't exist.</p>
          <button
            type="button"
            onClick={() => navigate(-1)}
            className="mt-6 text-sm font-medium text-primary hover:text-primary-hover transition-colors"
          >
            Go back
          </button>
        </div>
      </AnimatedPage>
    );
  }

  return (
    <AnimatedPage>
      <article className="mx-auto max-w-3xl">
        {/* Back link */}
        <motion.div initial={{ opacity: 0, x: -12 }} animate={{ opacity: 1, x: 0 }} className="mb-6">
          <button
            type="button"
            onClick={() => navigate(-1)}
            className="inline-flex items-center gap-1.5 rounded-full bg-surface/40 px-3 py-1.5 text-xs font-medium text-foreground-muted backdrop-blur-sm transition-colors hover:bg-surface/60 hover:text-foreground"
          >
            <svg className="h-3.5 w-3.5" fill="none" stroke="currentColor" viewBox="0 0 24 24" strokeWidth={2.5}>
              <path strokeLinecap="round" strokeLinejoin="round" d="M15.75 19.5L8.25 12l7.5-7.5" />
            </svg>
            Back
          </button>
        </motion.div>

        {/* Hero cover */}
        <motion.div
          initial={{ opacity: 0, y: 20 }}
          animate={{ opacity: 1, y: 0 }}
          className="overflow-hidden rounded-2xl"
        >
          {guide.coverImageUrl ? (
            <img
              src={guide.coverImageUrl}
              alt=""
              className="h-56 w-full object-cover sm:h-72"
            />
          ) : (
            <div className="flex h-56 items-center justify-center bg-linear-to-br from-primary/10 via-surface/60 to-accent/10 sm:h-72">
              <span className="text-6xl opacity-30">📖</span>
            </div>
          )}
        </motion.div>

        {/* Title & meta */}
        <div className="mt-6 space-y-4">
          <motion.h1
            initial={{ opacity: 0, y: 12 }}
            animate={{ opacity: 1, y: 0 }}
            transition={{ delay: 0.1 }}
            className="text-3xl font-extrabold tracking-tight text-foreground sm:text-4xl"
          >
            {guide.title}
          </motion.h1>

          <motion.div
            initial={{ opacity: 0, y: 8 }}
            animate={{ opacity: 1, y: 0 }}
            transition={{ delay: 0.15 }}
            className="flex flex-wrap items-center gap-4"
          >
            {/* Author */}
            <div className="flex items-center gap-2">
              <UserAvatar
                username={guide.authorUsername}
                avatarUrl={guide.authorAvatarUrl}
                size="sm"
              />
              <span className="text-sm font-medium text-foreground">{guide.authorUsername}</span>
            </div>

            <span className="text-xs text-foreground-subtle">{formatDate(guide.createdAt)}</span>

            {/* View count */}
            <span className="inline-flex items-center gap-1 text-xs text-foreground-subtle">
              <svg className="h-3.5 w-3.5" fill="none" stroke="currentColor" viewBox="0 0 24 24" strokeWidth={2}>
                <path strokeLinecap="round" strokeLinejoin="round" d="M2.036 12.322a1.012 1.012 0 010-.639C3.423 7.51 7.36 4.5 12 4.5c4.64 0 8.577 3.007 9.963 7.178.07.207.07.431 0 .639C20.577 16.49 16.64 19.5 12 19.5c-4.64 0-8.577-3.007-9.963-7.178z" />
                <path strokeLinecap="round" strokeLinejoin="round" d="M15 12a3 3 0 11-6 0 3 3 0 016 0z" />
              </svg>
              {guide.viewCount} views
            </span>

            <UpvoteButton
              targetId={guide.id}
              targetType={1}
              count={guide.upvoteCount}
              hasUpvoted={guide.hasUpvoted}
              size="sm"
            />
          </motion.div>
        </div>

        {/* Content body */}
        <motion.div
          initial={{ opacity: 0, y: 16 }}
          animate={{ opacity: 1, y: 0 }}
          transition={{ delay: 0.25 }}
          className="mt-8 rounded-2xl border border-border/50 bg-linear-to-br from-surface/80 to-surface/40 p-6 backdrop-blur-sm sm:p-8"
        >
          <div className="prose-sm prose-invert max-w-none whitespace-pre-wrap text-sm leading-relaxed text-foreground-muted">
            {guide.content}
          </div>
        </motion.div>

        {/* Tags */}
        {guide.tags.length > 0 && (
          <motion.div
            initial={{ opacity: 0, y: 12 }}
            animate={{ opacity: 1, y: 0 }}
            transition={{ delay: 0.35 }}
            className="mt-6 flex flex-wrap gap-2"
          >
            {guide.tags.map((tag, i) => (
              <motion.div
                key={tag}
                initial={{ opacity: 0, scale: 0.85 }}
                animate={{ opacity: 1, scale: 1 }}
                transition={{ delay: 0.4 + i * 0.03 }}
              >
                <Badge variant="default" className="px-3 py-1">{tag}</Badge>
              </motion.div>
            ))}
          </motion.div>
        )}

        {/* Bottom spacing */}
        <div className="h-12" />
      </article>
    </AnimatedPage>
  );
}
