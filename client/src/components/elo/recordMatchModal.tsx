import { useState } from 'react';
import { motion, AnimatePresence } from 'framer-motion';
import { useRecordMatch } from '@/features/elo/hooks';
import { useGameBrowse } from '@/features/games/hooks';
import { discoverPlayers } from '@/features/users/api';
import { UserAvatar } from '@/components/common/userAvatar';
import { Modal, Button, Select, Spinner } from '@/components/ui';
import { useToast } from '@/components/ui/toast';

interface RecordMatchModalProps {
  isOpen: boolean;
  onClose: () => void;
}

export function RecordMatchModal({ isOpen, onClose }: RecordMatchModalProps) {
  const [gameId, setGameId] = useState('');
  const [search, setSearch] = useState('');
  const [searching, setSearching] = useState(false);
  const [players, setPlayers] = useState<{ id: string; username: string; avatarUrl: string | null }[]>([]);
  const [selectedPlayer, setSelectedPlayer] = useState<{ id: string; username: string; avatarUrl: string | null } | null>(null);
  const [won, setWon] = useState<boolean | null>(null);

  const { data: gamesData } = useGameBrowse({ pageSize: 100 });
  const recordMatch = useRecordMatch();
  const { addToast } = useToast();

  const games = gamesData?.items ?? [];
  const gameOptions = [
    { value: '', label: 'Select a game...' },
    ...games.map((g) => ({ value: g.id, label: g.name })),
  ];

  const handleSearch = async () => {
    if (search.trim().length < 2) return;
    setSearching(true);
    try {
      const result = await discoverPlayers({ search: search.trim(), pageSize: 10 });
      setPlayers(result.items.map((p) => ({ id: p.id, username: p.username, avatarUrl: p.avatarUrl })));
    } catch {
      setPlayers([]);
    } finally {
      setSearching(false);
    }
  };

  const handleSubmit = async () => {
    if (!gameId || !selectedPlayer || won === null) return;
    try {
      await recordMatch.mutateAsync({ gameId, opponentId: selectedPlayer.id, won });
      addToast({ title: 'Match recorded successfully!', type: 'success' });
      handleReset();
      onClose();
    } catch {
      addToast({ title: 'Failed to record match', type: 'error' });
    }
  };

  const handleReset = () => {
    setGameId('');
    setSearch('');
    setPlayers([]);
    setSelectedPlayer(null);
    setWon(null);
  };

  const handleClose = () => {
    handleReset();
    onClose();
  };

  const isValid = !!gameId && !!selectedPlayer && won !== null;

  return (
    <Modal isOpen={isOpen} onClose={handleClose} title="Record Match Result" className="max-w-md">
      <div className="space-y-5">
        {/* Game Selection */}
        <div>
          <Select
            label="Game"
            options={gameOptions}
            value={gameId}
            onChange={(e) => setGameId(e.target.value)}
          />
        </div>

        {/* Opponent Search */}
        <div>
          <label className="mb-1.5 block text-sm font-medium text-foreground">Opponent</label>
          {selectedPlayer ? (
            <div className="flex items-center gap-3 rounded-lg border border-primary/30 bg-primary/5 p-3">
              <UserAvatar username={selectedPlayer.username} avatarUrl={selectedPlayer.avatarUrl} size="sm" />
              <span className="flex-1 font-medium text-foreground">{selectedPlayer.username}</span>
              <button
                onClick={() => setSelectedPlayer(null)}
                className="rounded-md p-1 text-foreground-muted hover:bg-surface-hover hover:text-foreground transition-colors"
              >
                <svg className="h-4 w-4" fill="none" stroke="currentColor" viewBox="0 0 24 24" strokeWidth={2}>
                  <path strokeLinecap="round" strokeLinejoin="round" d="M6 18L18 6M6 6l12 12" />
                </svg>
              </button>
            </div>
          ) : (
            <>
              <div className="flex gap-2">
                <input
                  type="text"
                  placeholder="Search by username..."
                  value={search}
                  onChange={(e) => setSearch(e.target.value)}
                  onKeyDown={(e) => e.key === 'Enter' && handleSearch()}
                  className="flex-1 rounded-lg border border-border bg-surface-hover px-3 py-2 text-sm text-foreground placeholder:text-foreground-subtle focus:outline-none focus:ring-2 focus:ring-primary/50"
                />
                <Button size="sm" onClick={handleSearch} isLoading={searching}>
                  Search
                </Button>
              </div>

              {searching && (
                <div className="flex justify-center py-3">
                  <Spinner size="sm" />
                </div>
              )}

              <AnimatePresence>
                {players.length > 0 && (
                  <motion.div
                    initial={{ opacity: 0, height: 0 }}
                    animate={{ opacity: 1, height: 'auto' }}
                    exit={{ opacity: 0, height: 0 }}
                    className="mt-2 max-h-48 space-y-1 overflow-y-auto rounded-lg border border-border"
                  >
                    {players.map((player) => (
                      <button
                        key={player.id}
                        onClick={() => {
                          setSelectedPlayer(player);
                          setPlayers([]);
                          setSearch('');
                        }}
                        className="flex w-full items-center gap-3 px-3 py-2 text-left transition-colors hover:bg-surface-hover"
                      >
                        <UserAvatar username={player.username} avatarUrl={player.avatarUrl} size="sm" />
                        <span className="truncate text-sm font-medium text-foreground">{player.username}</span>
                      </button>
                    ))}
                  </motion.div>
                )}
              </AnimatePresence>

              {!searching && search.length >= 2 && players.length === 0 && (
                <p className="mt-2 text-center text-xs text-foreground-muted">No players found</p>
              )}
            </>
          )}
        </div>

        {/* Result Toggle */}
        <div>
          <label className="mb-1.5 block text-sm font-medium text-foreground">Result</label>
          <div className="grid grid-cols-2 gap-3">
            <button
              onClick={() => setWon(true)}
              className={`flex items-center justify-center gap-2 rounded-lg border-2 px-4 py-3 text-sm font-semibold transition-all ${
                won === true
                  ? 'border-success bg-success/10 text-success'
                  : 'border-border text-foreground-muted hover:border-success/50 hover:text-success'
              }`}
            >
              <span className="text-lg">🏆</span>
              Victory
            </button>
            <button
              onClick={() => setWon(false)}
              className={`flex items-center justify-center gap-2 rounded-lg border-2 px-4 py-3 text-sm font-semibold transition-all ${
                won === false
                  ? 'border-danger bg-danger/10 text-danger'
                  : 'border-border text-foreground-muted hover:border-danger/50 hover:text-danger'
              }`}
            >
              <span className="text-lg">💀</span>
              Defeat
            </button>
          </div>
        </div>

        {/* Submit */}
        <div className="flex justify-end gap-3 pt-2">
          <Button variant="ghost" onClick={handleClose}>Cancel</Button>
          <Button
            onClick={handleSubmit}
            disabled={!isValid}
            isLoading={recordMatch.isPending}
          >
            Record Match
          </Button>
        </div>
      </div>
    </Modal>
  );
}
