import { useEffect, useMemo, useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { motion } from 'framer-motion';
import { Button, Input, Select, Textarea } from '@/components/ui';
import { useToast } from '@/components/ui/toast';
import { useGameBrowse } from '@/features/games/hooks';
import { useCreateTopic } from '@/features/community/hooks';
import type { CommunityBoardResponse } from '@/features/community/types';

const GENERAL_CATEGORIES = ['Looking for Team', 'Debate', 'Strategy', 'Highlights'];
const GAME_CATEGORIES = ['Meta', 'Looking for Team', 'Patch Talk', 'Strategy'];
const TITLE_MAX = 140;
const CONTENT_MAX = 1000;

interface TopicComposerProps {
  boardId: string;
  boardCategory?: string;
  boards?: CommunityBoardResponse[];
}

export function TopicComposer({ boardId, boardCategory, boards = [] }: TopicComposerProps) {
  const navigate = useNavigate();
  const { addToast } = useToast();
  const createTopic = useCreateTopic();
  const { data: gamesPage } = useGameBrowse({ page: 1, pageSize: 80 });
  const categories = useMemo(
    () => (boardCategory?.toLowerCase() === 'game' ? GAME_CATEGORIES : GENERAL_CATEGORIES),
    [boardCategory],
  );
  const [title, setTitle] = useState('');
  const [content, setContent] = useState('');
  const [category, setCategory] = useState(categories[0]);
  const [imageUrl, setImageUrl] = useState('');
  const [gameId, setGameId] = useState('');

  useEffect(() => {
    setCategory(categories[0]);
  }, [categories]);

  const gameOptions = useMemo(() => {
    const items = gamesPage?.items ?? [];
    return items.map((g) => ({ value: g.id, label: g.name }));
  }, [gamesPage]);

  const resolveBoardIdForGame = (gid: string) => boards.find((b) => b.gameId === gid)?.id;

  const canSubmit =
    title.trim().length > 2 &&
    title.trim().length <= TITLE_MAX &&
    content.trim().length > 10 &&
    content.trim().length <= CONTENT_MAX;

  const handleSubmit = () => {
    if (!canSubmit) return;

    let postBoardId = boardId;
    if (gameId) {
      const mapped = resolveBoardIdForGame(gameId);
      if (!mapped) {
        addToast({
          title: 'No board for this game',
          message: 'Create a game-tied board from Community home first, or clear the game and post here.',
          type: 'error',
        });
        return;
      }
      postBoardId = mapped;
    }

    const trimmedImage = imageUrl.trim();
    createTopic.mutate(
      {
        title: title.trim(),
        content: content.trim(),
        category,
        boardId: postBoardId,
        imageUrl: trimmedImage || undefined,
      },
      {
        onSuccess: (data) => {
          addToast({ title: 'Topic published!', message: 'Your new thread is live.', type: 'success' });
          setTitle('');
          setContent('');
          setImageUrl('');
          setGameId('');
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

  const previewBg = imageUrl.trim() || undefined;

  return (
    <motion.section
      initial={{ opacity: 0, y: 16 }}
      animate={{ opacity: 1, y: 0 }}
      transition={{ delay: 0.12 }}
      className="relative overflow-hidden rounded-[28px] border border-border/50 bg-surface/70 p-5 backdrop-blur-md sm:p-6"
    >
      {previewBg ? (
        <div
          className="pointer-events-none absolute inset-0 bg-cover bg-center opacity-[0.18]"
          style={{ backgroundImage: `url(${previewBg})` }}
        />
      ) : null}
      <div className="absolute inset-0 bg-[radial-gradient(circle_at_top_right,rgba(6,214,160,0.08),transparent_35%)]" />
      <div className="relative">
        <div className="flex flex-col gap-2 md:flex-row md:items-start md:justify-between">
          <div>
            <p className="text-[11px] font-semibold uppercase tracking-[0.22em] text-accent/90">
              New topic
            </p>
          </div>
          <p className="max-w-md text-xs leading-relaxed text-foreground-subtle md:text-right">
            Optional: tag someone with <span className="font-semibold text-foreground-muted">@username</span>
            {' '}— only if you want to notify them; not required to post.
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

          {gameOptions.length > 0 ? (
            <Select
              label="Game (optional)"
              placeholder="Post in this board only"
              value={gameId}
              onChange={(e) => setGameId(e.target.value)}
              options={gameOptions}
            />
          ) : null}

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
            <Input
              value={imageUrl}
              onChange={(event) => setImageUrl(event.target.value.slice(0, 600))}
              placeholder="Cover image URL (optional)"
              className="border-border/60 bg-background/60"
            />
          </div>

          <div>
            <Textarea
              value={content}
              onChange={(event) => setContent(event.target.value.slice(0, CONTENT_MAX))}
              placeholder="Write your post…"
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
