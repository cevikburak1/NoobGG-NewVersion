import { useEffect, useMemo, useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { motion } from 'framer-motion';
import { Button, Input, Textarea } from '@/components/ui';
import { useToast } from '@/components/ui/toast';
import { useCreateTopic } from '@/features/community/hooks';
import type { CommunityBoardType } from '@/features/community/types';

const GENERAL_CATEGORIES = ['Looking for Team', 'Debate', 'Strategy', 'Highlights'];
const GAME_CATEGORIES = ['Meta', 'Looking for Team', 'Patch Talk', 'Strategy'];
const TITLE_MAX = 140;
const CONTENT_MAX = 1000;

interface TopicComposerProps {
  boardType: CommunityBoardType;
  gameId?: string | null;
  boardName: string;
}

export function TopicComposer({ boardType, gameId, boardName }: TopicComposerProps) {
  const navigate = useNavigate();
  const { addToast } = useToast();
  const createTopic = useCreateTopic();
  const categories = useMemo(
    () => (boardType === 'General' ? GENERAL_CATEGORIES : GAME_CATEGORIES),
    [boardType],
  );
  const [title, setTitle] = useState('');
  const [content, setContent] = useState('');
  const [category, setCategory] = useState(categories[0]);

  useEffect(() => {
    setCategory(categories[0]);
  }, [categories]);

  const canSubmit =
    title.trim().length > 2 &&
    title.trim().length <= TITLE_MAX &&
    content.trim().length > 10 &&
    content.trim().length <= CONTENT_MAX;

  const handleSubmit = () => {
    if (!canSubmit) return;

    createTopic.mutate(
      {
        title: title.trim(),
        content: content.trim(),
        category,
        boardType,
        gameId: gameId ?? undefined,
      },
      {
        onSuccess: (data) => {
          addToast({ title: 'Topic published!', message: 'Your new thread is live.', type: 'success' });
          setTitle('');
          setContent('');
          setCategory(categories[0]);
          if (data?.id) {
            navigate(`/community/topics/${data.id}`);
          }
        },
        onError: () => {
          addToast({ title: 'Could not publish', message: 'Something went wrong. Try again.', type: 'error' });
        },
      },
    );
  };

  return (
    <motion.section
      initial={{ opacity: 0, y: 16 }}
      animate={{ opacity: 1, y: 0 }}
      transition={{ delay: 0.12 }}
      className="relative overflow-hidden rounded-[28px] border border-border/50 bg-surface/70 p-5 backdrop-blur-md sm:p-6"
    >
      <div className="absolute inset-0 bg-[radial-gradient(circle_at_top_right,rgba(6,214,160,0.08),transparent_35%)]" />
      <div className="relative">
        <div className="flex flex-col gap-3 md:flex-row md:items-end md:justify-between">
          <div>
            <p className="text-[11px] font-semibold uppercase tracking-[0.22em] text-accent/90">
              Open a new topic
            </p>
            <h2
              className="mt-2 text-2xl font-bold tracking-[-0.04em] text-foreground"
              style={{ fontFamily: "'Bricolage Grotesque', 'Plus Jakarta Sans', var(--font-sans)" }}
            >
              Start the next conversation in {boardName}
            </h2>
          </div>
          <p className="max-w-xs text-xs leading-6 text-foreground-subtle">
            Mention players with <span className="font-semibold text-primary">@username</span> to notify them directly.
          </p>
        </div>

        <div className="mt-5 space-y-4">
          <div>
            <Input
              value={title}
              onChange={(event) => setTitle(event.target.value.slice(0, TITLE_MAX))}
              placeholder="Topic title"
              className="border-border/60 bg-background/60"
            />
            <p className="mt-1 text-right text-[11px] text-foreground-subtle">
              {title.trim().length}/{TITLE_MAX}
            </p>
          </div>

          <div className="flex flex-wrap gap-2">
            {categories.map((item) => (
              <button
                key={item}
                type="button"
                onClick={() => setCategory(item)}
                className={`rounded-full px-3 py-1.5 text-xs font-medium transition-colors ${
                  category === item
                    ? 'bg-primary text-primary-foreground'
                    : 'border border-border/60 bg-surface/60 text-foreground-muted hover:text-foreground'
                }`}
              >
                {item}
              </button>
            ))}
          </div>

          <div>
            <Textarea
              value={content}
              onChange={(event) => setContent(event.target.value.slice(0, CONTENT_MAX))}
              placeholder="Drop the full take: the patch, the roster need, the strategy question, or the thing you want the board to react to."
              rows={5}
              className="border-border/60 bg-background/60"
            />
            <p className="mt-1 text-right text-[11px] text-foreground-subtle">
              {content.trim().length}/{CONTENT_MAX}
            </p>
          </div>

          <div className="flex flex-col gap-3 sm:flex-row sm:items-center sm:justify-end">
            <Button
              onClick={handleSubmit}
              disabled={!canSubmit}
              isLoading={createTopic.isPending}
              className="min-w-36"
            >
              Publish Topic
            </Button>
          </div>
        </div>
      </div>
    </motion.section>
  );
}
