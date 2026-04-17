import { useState } from 'react';
import { Link } from 'react-router-dom';
import { motion, AnimatePresence } from 'framer-motion';
import { Button, Textarea, Spinner, staggerContainer, staggerItem } from '@/components/ui';
import { UserAvatar } from '@/components/common/userAvatar';
import { UpvoteButton } from './upvoteButton';
import {
  useCommunityFeed,
  useCreatePost,
  usePostComments,
  useAddComment,
} from '@/features/community/hooks';
import type { CommunityPostResponse } from '@/features/community/types';

const GLASS_CARD =
  'rounded-2xl border border-border/50 bg-linear-to-br from-surface/80 to-surface/40 p-5 backdrop-blur-sm';

function relativeTime(dateStr: string): string {
  const diff = Date.now() - new Date(dateStr).getTime();
  const mins = Math.floor(diff / 60_000);
  if (mins < 1) return 'just now';
  if (mins < 60) return `${mins}m ago`;
  const hours = Math.floor(mins / 60);
  if (hours < 24) return `${hours}h ago`;
  const days = Math.floor(hours / 24);
  if (days < 30) return `${days}d ago`;
  return new Date(dateStr).toLocaleDateString('en-US', { month: 'short', day: 'numeric' });
}

interface CommunityFeedProps {
  gameId: string;
}

export function CommunityFeed({ gameId }: CommunityFeedProps) {
  const [page, setPage] = useState(1);
  const { data, isLoading } = useCommunityFeed(gameId, page);
  const createPost = useCreatePost();
  const [postContent, setPostContent] = useState('');

  const handleSubmitPost = () => {
    const trimmed = postContent.trim();
    if (!trimmed) return;
    createPost.mutate({ gameId, content: trimmed }, {
      onSuccess: () => setPostContent(''),
    });
  };

  return (
    <div className="space-y-5">
      {/* Post creation */}
      <motion.div
        initial={{ opacity: 0, y: 16 }}
        animate={{ opacity: 1, y: 0 }}
        className={GLASS_CARD}
      >
        <Textarea
          placeholder="What's on your mind?"
          value={postContent}
          onChange={(e) => setPostContent(e.target.value)}
          rows={3}
          className="border-border/30 bg-surface/40"
        />
        <div className="mt-3 flex justify-end">
          <Button
            size="sm"
            onClick={handleSubmitPost}
            disabled={!postContent.trim()}
            isLoading={createPost.isPending}
          >
            Post
          </Button>
        </div>
      </motion.div>

      {/* Feed */}
      {isLoading ? (
        <div className="flex justify-center py-16">
          <Spinner size="lg" />
        </div>
      ) : !data?.posts.length ? (
        <motion.div
          initial={{ opacity: 0, scale: 0.95 }}
          animate={{ opacity: 1, scale: 1 }}
          className="flex flex-col items-center py-20 text-center"
        >
          <span className="text-5xl">💬</span>
          <p className="mt-4 text-lg font-semibold text-foreground">No posts yet</p>
          <p className="mt-1 text-sm text-foreground-muted">Be the first to share!</p>
        </motion.div>
      ) : (
        <>
          <motion.div
            variants={staggerContainer}
            initial="hidden"
            animate="show"
            className="space-y-4"
          >
            {data.posts.map((post) => (
              <motion.div key={post.id} variants={staggerItem}>
                <PostCard post={post} />
              </motion.div>
            ))}
          </motion.div>

          {/* Pagination */}
          {data.hasMore && (
            <div className="flex justify-center pt-2">
              <Button
                variant="ghost"
                size="sm"
                onClick={() => setPage((p) => p + 1)}
              >
                Load more
              </Button>
            </div>
          )}
        </>
      )}
    </div>
  );
}

function PostCard({ post }: { post: CommunityPostResponse }) {
  const [showComments, setShowComments] = useState(false);

  return (
    <div className={GLASS_CARD}>
      {/* Author row */}
      <div className="flex items-center gap-3">
        <UserAvatar
          username={post.authorUsername}
          avatarUrl={post.authorAvatarUrl}
          size="sm"
        />
        <div className="min-w-0 flex-1">
          <p className="truncate text-sm font-semibold text-foreground">{post.authorUsername}</p>
          <p className="text-xs text-foreground-subtle">{relativeTime(post.createdAt)}</p>
        </div>
      </div>

      {/* Content */}
      <Link to={`/community/topics/${post.id}`} className="block">
        <h3 className="mt-3 text-lg font-bold text-foreground transition-colors hover:text-primary">
          {post.title}
        </h3>
      </Link>
      <p className="mt-3 whitespace-pre-wrap text-sm leading-relaxed text-foreground-muted">
        {post.content}
      </p>

      {post.imageUrl && (
        <img
          src={post.imageUrl}
          alt=""
          className="mt-3 max-h-80 w-full rounded-xl object-cover"
        />
      )}

      {/* Actions */}
      <div className="mt-4 flex items-center gap-3">
        <UpvoteButton
          targetId={post.id}
          targetType={0}
          count={post.upvoteCount}
          hasUpvoted={post.hasUpvoted}
          size="sm"
        />
        <button
          type="button"
          onClick={() => setShowComments((v) => !v)}
          className="inline-flex items-center gap-1.5 rounded-lg border border-transparent bg-surface-hover/60 px-2 py-1 text-xs font-medium text-foreground-muted transition-colors hover:border-border/50 hover:text-foreground"
        >
          <svg className="h-3.5 w-3.5" fill="none" stroke="currentColor" viewBox="0 0 24 24" strokeWidth={2}>
            <path strokeLinecap="round" strokeLinejoin="round" d="M8.625 12a.375.375 0 11-.75 0 .375.375 0 01.75 0zm0 0H8.25m4.125 0a.375.375 0 11-.75 0 .375.375 0 01.75 0zm0 0H12m4.125 0a.375.375 0 11-.75 0 .375.375 0 01.75 0zm0 0h-.375M21 12c0 4.556-4.03 8.25-9 8.25a9.764 9.764 0 01-2.555-.337A5.972 5.972 0 015.41 20.97a5.969 5.969 0 01-2.41-.5v.03a.75.75 0 01-.75.75h-.03A8.256 8.256 0 013 12c0-4.556 4.03-8.25 9-8.25s9 3.694 9 8.25z" />
          </svg>
          {post.commentCount}
        </button>
      </div>

      {/* Comments section */}
      <AnimatePresence>
        {showComments && (
          <motion.div
            initial={{ height: 0, opacity: 0 }}
            animate={{ height: 'auto', opacity: 1 }}
            exit={{ height: 0, opacity: 0 }}
            transition={{ duration: 0.25 }}
            className="overflow-hidden"
          >
            <CommentSection postId={post.id} />
          </motion.div>
        )}
      </AnimatePresence>
    </div>
  );
}

function CommentSection({ postId }: { postId: string }) {
  const { data, isLoading } = usePostComments(postId);
  const addComment = useAddComment();
  const [text, setText] = useState('');

  const handleSubmit = () => {
    const trimmed = text.trim();
    if (!trimmed) return;
    addComment.mutate({ postId, content: trimmed }, {
      onSuccess: () => setText(''),
    });
  };

  return (
    <div className="mt-4 space-y-3 border-t border-border/30 pt-4">
      {/* Comment input */}
      <div className="flex gap-2">
        <Textarea
          placeholder="Write a comment..."
          value={text}
          onChange={(e) => setText(e.target.value)}
          rows={1}
          className="flex-1 border-border/30 bg-surface/40 text-xs"
        />
        <Button
          size="sm"
          onClick={handleSubmit}
          disabled={!text.trim()}
          isLoading={addComment.isPending}
        >
          Reply
        </Button>
      </div>

      {isLoading ? (
        <div className="flex justify-center py-4">
          <Spinner size="sm" />
        </div>
      ) : !data?.comments.length ? (
        <p className="py-3 text-center text-xs text-foreground-subtle">No comments yet</p>
      ) : (
        <div className="space-y-2">
          {data.comments.map((comment) => (
            <motion.div
              key={comment.id}
              initial={{ opacity: 0, x: -8 }}
              animate={{ opacity: 1, x: 0 }}
              className="flex gap-2 rounded-xl bg-surface/40 p-3"
            >
              <UserAvatar
                username={comment.authorUsername}
                avatarUrl={comment.authorAvatarUrl}
                size="xs"
              />
              <div className="min-w-0 flex-1">
                <div className="flex items-center gap-2">
                  <span className="truncate text-xs font-semibold text-foreground">{comment.authorUsername}</span>
                  <span className="shrink-0 text-[10px] text-foreground-subtle">{relativeTime(comment.createdAt)}</span>
                </div>
                <p className="mt-0.5 text-xs leading-relaxed text-foreground-muted">{comment.content}</p>
                <div className="mt-1.5">
                  <UpvoteButton
                    targetId={comment.id}
                    targetType={2}
                    count={comment.upvoteCount}
                    hasUpvoted={comment.hasUpvoted}
                    size="sm"
                  />
                </div>
              </div>
            </motion.div>
          ))}
        </div>
      )}
    </div>
  );
}
