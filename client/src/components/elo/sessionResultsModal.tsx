import { useState } from 'react';
import { useSubmitSessionResults } from '@/features/elo/hooks';
import { Modal, Button, Spinner } from '@/components/ui';
import { RankBadge } from './rankBadge';

interface SessionResultsModalProps {
  isOpen: boolean;
  roomId: string;
  gameName: string | null;
  gameImageUrl: string | null;
  averageRankTier: string | null;
  onDone: () => void;
  onSkip: () => void;
}

export function SessionResultsModal({
  isOpen,
  roomId,
  gameName,
  gameImageUrl,
  averageRankTier,
  onDone,
  onSkip,
}: SessionResultsModalProps) {
  const [wins, setWins] = useState(0);
  const [losses, setLosses] = useState(0);
  const submitResults = useSubmitSessionResults();

  const totalMatches = wins + losses;
  const canSubmit = totalMatches > 0 && !submitResults.isPending;

  const handleSubmit = async () => {
    try {
      await submitResults.mutateAsync({ roomId, wins, losses });
      onDone();
    } catch {
      onDone();
    }
  };

  return (
    <Modal isOpen={isOpen} onClose={onSkip} title="Session Results" className="max-w-sm">
      <div className="space-y-5">
        <div className="flex items-center gap-3 rounded-lg border border-border bg-surface-hover p-3">
          {gameImageUrl ? (
            <img src={gameImageUrl} alt={gameName ?? ''} className="h-10 w-14 shrink-0 rounded-lg object-cover" />
          ) : (
            <div className="flex h-10 w-14 shrink-0 items-center justify-center rounded-lg bg-surface text-lg">
              🎮
            </div>
          )}
          <div className="min-w-0 flex-1">
            <p className="truncate font-medium text-foreground">{gameName ?? 'Unknown Game'}</p>
            {averageRankTier && (
              <div className="mt-0.5">
                <RankBadge tier={averageRankTier} size="sm" />
              </div>
            )}
          </div>
        </div>

        <p className="text-sm text-foreground-muted">
          How did your session go? Enter your match results to update your ranking.
        </p>

        <div className="grid grid-cols-2 gap-4">
          <div>
            <label className="mb-1.5 block text-sm font-medium text-success">Wins</label>
            <div className="flex items-center gap-2">
              <button
                onClick={() => setWins(Math.max(0, wins - 1))}
                className="flex h-9 w-9 items-center justify-center rounded-lg border border-border text-foreground-muted hover:bg-surface-hover transition-colors"
              >
                -
              </button>
              <span className="min-w-8 text-center text-xl font-bold text-success">{wins}</span>
              <button
                onClick={() => setWins(wins + 1)}
                className="flex h-9 w-9 items-center justify-center rounded-lg border border-border text-foreground-muted hover:bg-surface-hover transition-colors"
              >
                +
              </button>
            </div>
          </div>

          <div>
            <label className="mb-1.5 block text-sm font-medium text-danger">Losses</label>
            <div className="flex items-center gap-2">
              <button
                onClick={() => setLosses(Math.max(0, losses - 1))}
                className="flex h-9 w-9 items-center justify-center rounded-lg border border-border text-foreground-muted hover:bg-surface-hover transition-colors"
              >
                -
              </button>
              <span className="min-w-8 text-center text-xl font-bold text-danger">{losses}</span>
              <button
                onClick={() => setLosses(losses + 1)}
                className="flex h-9 w-9 items-center justify-center rounded-lg border border-border text-foreground-muted hover:bg-surface-hover transition-colors"
              >
                +
              </button>
            </div>
          </div>
        </div>

        {totalMatches > 0 && (
          <div className="rounded-lg bg-surface-hover px-3 py-2 text-center text-sm text-foreground-muted">
            {totalMatches} match{totalMatches !== 1 ? 'es' : ''} total
            {wins > 0 && losses > 0 && ` — ${Math.round((wins / totalMatches) * 100)}% win rate`}
          </div>
        )}

        {submitResults.isPending && (
          <div className="flex justify-center">
            <Spinner size="sm" />
          </div>
        )}

        <div className="flex justify-end gap-3">
          <Button variant="ghost" onClick={onSkip} disabled={submitResults.isPending}>
            Skip
          </Button>
          <Button onClick={handleSubmit} disabled={!canSubmit} isLoading={submitResults.isPending}>
            Submit Results
          </Button>
        </div>
      </div>
    </Modal>
  );
}
