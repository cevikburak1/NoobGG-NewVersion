import { useState } from 'react';
import { Link } from 'react-router-dom';
import { motion } from 'framer-motion';
import {
  Button,
  Badge,
  Spinner,
  Modal,
  Input,
  Textarea,
  staggerContainer,
  staggerItem,
} from '@/components/ui';
import { UserAvatar } from '@/components/common/userAvatar';
import { UpvoteButton } from '@/components/community/upvoteButton';
import { useGuides, useCreateGuide } from '@/features/guides/hooks';
import type { GuideListItemResponse } from '@/features/guides/types';
import { useToast } from '@/components/ui/toast';

const GLASS_CARD =
  'rounded-2xl border border-border/50 bg-linear-to-br from-surface/80 to-surface/40 backdrop-blur-sm';

type SortOption = 'recent' | 'popular';

interface GuideListProps {
  gameId: string;
}

export function GuideList({ gameId }: GuideListProps) {
  const [sortBy, setSortBy] = useState<SortOption>('recent');
  const [page, setPage] = useState(1);
  const { data, isLoading } = useGuides(gameId, sortBy, page);
  const [showCreateModal, setShowCreateModal] = useState(false);

  return (
    <div className="space-y-5">
      {/* Header */}
      <motion.div
        initial={{ opacity: 0, y: 12 }}
        animate={{ opacity: 1, y: 0 }}
        className="flex items-center justify-between"
      >
        {/* Sort pills */}
        <div className="flex gap-1 rounded-lg border border-border/50 bg-surface/60 p-1 backdrop-blur-sm">
          {(['recent', 'popular'] as const).map((opt) => (
            <button
              key={opt}
              type="button"
              onClick={() => { setSortBy(opt); setPage(1); }}
              className={`relative rounded-md px-3.5 py-1.5 text-xs font-semibold transition-colors ${
                sortBy === opt ? 'text-foreground' : 'text-foreground-muted hover:text-foreground'
              }`}
            >
              {sortBy === opt && (
                <motion.div
                  layoutId="guideSortPill"
                  className="absolute inset-0 rounded-md bg-surface-hover"
                  transition={{ type: 'spring', bounce: 0.2, duration: 0.4 }}
                />
              )}
              <span className="relative z-10 capitalize">{opt}</span>
            </button>
          ))}
        </div>

        <Button size="sm" onClick={() => setShowCreateModal(true)}>
          Write a Guide
        </Button>
      </motion.div>

      {/* Grid */}
      {isLoading ? (
        <div className="flex justify-center py-16">
          <Spinner size="lg" />
        </div>
      ) : !data?.guides.length ? (
        <motion.div
          initial={{ opacity: 0, scale: 0.95 }}
          animate={{ opacity: 1, scale: 1 }}
          className="flex flex-col items-center py-20 text-center"
        >
          <span className="text-5xl">📖</span>
          <p className="mt-4 text-lg font-semibold text-foreground">No guides yet</p>
          <p className="mt-1 text-sm text-foreground-muted">Write the first guide!</p>
        </motion.div>
      ) : (
        <>
          <motion.div
            variants={staggerContainer}
            initial="hidden"
            animate="show"
            className="grid gap-4 sm:grid-cols-2"
          >
            {data.guides.map((guide) => (
              <motion.div key={guide.id} variants={staggerItem}>
                <GuideCard guide={guide} />
              </motion.div>
            ))}
          </motion.div>

          {data.hasMore && (
            <div className="flex justify-center pt-2">
              <Button variant="ghost" size="sm" onClick={() => setPage((p) => p + 1)}>
                Load more
              </Button>
            </div>
          )}
        </>
      )}

      {/* Create guide modal */}
      <CreateGuideModal
        gameId={gameId}
        isOpen={showCreateModal}
        onClose={() => setShowCreateModal(false)}
      />
    </div>
  );
}

function GuideCard({ guide }: { guide: GuideListItemResponse }) {
  return (
    <Link to={`/guides/${guide.id}`} className="group block">
      <div className={`${GLASS_CARD} overflow-hidden transition-colors hover:border-primary/30`}>
        {/* Cover */}
        {guide.coverImageUrl ? (
          <img
            src={guide.coverImageUrl}
            alt=""
            className="h-36 w-full object-cover transition-transform duration-300 group-hover:scale-[1.02]"
          />
        ) : (
          <div className="flex h-36 items-center justify-center bg-linear-to-br from-primary/10 via-surface/60 to-accent/10">
            <span className="text-4xl opacity-40">📖</span>
          </div>
        )}

        <div className="p-4">
          {/* Title */}
          <h3 className="line-clamp-2 font-semibold text-foreground group-hover:text-primary transition-colors">
            {guide.title}
          </h3>

          {/* Author */}
          <div className="mt-2 flex items-center gap-2">
            <UserAvatar
              username={guide.authorUsername}
              avatarUrl={guide.authorAvatarUrl}
              size="xs"
            />
            <span className="truncate text-xs text-foreground-muted">{guide.authorUsername}</span>
          </div>

          {/* Tags */}
          {guide.tags.length > 0 && (
            <div className="mt-2.5 flex flex-wrap gap-1">
              {guide.tags.slice(0, 3).map((tag) => (
                <Badge key={tag} variant="default" className="text-[10px]">{tag}</Badge>
              ))}
              {guide.tags.length > 3 && (
                <span className="text-[10px] text-foreground-subtle">+{guide.tags.length - 3}</span>
              )}
            </div>
          )}

          {/* Stats */}
          <div className="mt-3 flex items-center gap-3">
            <UpvoteButton
              targetId={guide.id}
              targetType={1}
              count={guide.upvoteCount}
              hasUpvoted={guide.hasUpvoted}
              size="sm"
            />
            <span className="inline-flex items-center gap-1 text-xs text-foreground-subtle">
              <svg className="h-3.5 w-3.5" fill="none" stroke="currentColor" viewBox="0 0 24 24" strokeWidth={2}>
                <path strokeLinecap="round" strokeLinejoin="round" d="M2.036 12.322a1.012 1.012 0 010-.639C3.423 7.51 7.36 4.5 12 4.5c4.64 0 8.577 3.007 9.963 7.178.07.207.07.431 0 .639C20.577 16.49 16.64 19.5 12 19.5c-4.64 0-8.577-3.007-9.963-7.178z" />
                <path strokeLinecap="round" strokeLinejoin="round" d="M15 12a3 3 0 11-6 0 3 3 0 016 0z" />
              </svg>
              {guide.viewCount}
            </span>
          </div>
        </div>
      </div>
    </Link>
  );
}

function CreateGuideModal({
  gameId,
  isOpen,
  onClose,
}: {
  gameId: string;
  isOpen: boolean;
  onClose: () => void;
}) {
  const create = useCreateGuide();
  const { addToast } = useToast();
  const [title, setTitle] = useState('');
  const [content, setContent] = useState('');
  const [tagsInput, setTagsInput] = useState('');

  const handleSubmit = () => {
    const trimmedTitle = title.trim();
    const trimmedContent = content.trim();
    if (!trimmedTitle || !trimmedContent) return;

    const tags = tagsInput
      .split(',')
      .map((t) => t.trim())
      .filter(Boolean);

    create.mutate(
      { gameId, title: trimmedTitle, content: trimmedContent, tags },
      {
        onSuccess: () => {
          setTitle('');
          setContent('');
          setTagsInput('');
          onClose();
          addToast({ title: 'Guide published!', message: 'Your guide is now live.', type: 'success' });
        },
        onError: () => {
          addToast({ title: 'Failed to create guide', message: 'Please try again.', type: 'error' });
        },
      },
    );
  };

  return (
    <Modal isOpen={isOpen} onClose={onClose} title="Write a Guide" className="max-w-lg">
      <div className="space-y-4">
        <Input
          label="Title"
          placeholder="Your guide title"
          value={title}
          onChange={(e) => setTitle(e.target.value)}
        />
        <Textarea
          label="Content"
          placeholder="Share your knowledge..."
          value={content}
          onChange={(e) => setContent(e.target.value)}
          rows={8}
        />
        <Input
          label="Tags (comma separated)"
          placeholder="e.g. tips, beginner, ranked"
          value={tagsInput}
          onChange={(e) => setTagsInput(e.target.value)}
        />
        <div className="flex justify-end gap-2 pt-2">
          <Button variant="ghost" size="sm" onClick={onClose}>
            Cancel
          </Button>
          <Button
            size="sm"
            onClick={handleSubmit}
            disabled={!title.trim() || !content.trim()}
            isLoading={create.isPending}
          >
            Publish
          </Button>
        </div>
      </div>
    </Modal>
  );
}
