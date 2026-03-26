import { useState } from 'react';
import { Link } from 'react-router-dom';
import { motion, AnimatePresence } from 'framer-motion';
import { useGameBrowse } from '@/features/games/hooks';
import { useDiscoverPlayers } from '@/features/users/hooks';
import { useDebounce } from '@/hooks/useDebounce';
import { Button, Input, Badge, AnimatedPage, staggerContainer, staggerItem } from '@/components/ui';
import { UserAvatar } from '@/components/common/userAvatar';
import { GameCard } from '@/components/common/gameCard';
import type { DiscoverPlayerResponse } from '@/features/users/types';

const regions = ['All', 'EU', 'NA', 'TR', 'AS', 'SA', 'OCE', 'ME', 'AF', 'CIS', 'SEA'];
const levels = ['All', 'Beginner', 'Intermediate', 'Advanced', 'Expert'];

export default function DiscoverPage() {
  const [searchQuery, setSearchQuery] = useState('');
  const [selectedRegion, setSelectedRegion] = useState('All');
  const [selectedLevel, setSelectedLevel] = useState('All');
  const [activeTab, setActiveTab] = useState<'players' | 'games'>('games');
  const [onlyLFT, setOnlyLFT] = useState(false);
  const [gamePage, setGamePage] = useState(1);
  const [playerPage, setPlayerPage] = useState(1);

  const debouncedSearch = useDebounce(searchQuery, 300);

  const { data: gamesResult, isLoading: gamesLoading } = useGameBrowse({
    search: debouncedSearch || undefined,
    page: gamePage,
    pageSize: 12,
  });

  const { data: playersResult, isLoading: playersLoading } = useDiscoverPlayers({
    search: debouncedSearch || undefined,
    region: selectedRegion !== 'All' ? selectedRegion : undefined,
    experienceLevel: selectedLevel !== 'All' ? selectedLevel : undefined,
    lookingForTeam: onlyLFT || undefined,
    page: playerPage,
    pageSize: 12,
  });

  const handleSearchChange = (val: string) => {
    setSearchQuery(val);
    setGamePage(1);
    setPlayerPage(1);
  };

  return (
    <AnimatedPage>
      <div className="space-y-6">
        <div>
          <motion.h1
            initial={{ opacity: 0, x: -20 }}
            animate={{ opacity: 1, x: 0 }}
            className="text-3xl font-bold text-foreground"
          >
            Discover
          </motion.h1>
          <motion.p
            initial={{ opacity: 0, x: -20 }}
            animate={{ opacity: 1, x: 0 }}
            transition={{ delay: 0.05 }}
            className="mt-1 text-foreground-muted"
          >
            Find players and games that match your style
          </motion.p>
        </div>

        <motion.div
          initial={{ opacity: 0, y: 10 }}
          animate={{ opacity: 1, y: 0 }}
          transition={{ delay: 0.1 }}
          className="flex gap-1 rounded-lg border border-border bg-surface p-1"
        >
          {(['games', 'players'] as const).map((tab) => (
            <button
              key={tab}
              onClick={() => setActiveTab(tab)}
              className={`relative flex-1 rounded-md px-4 py-2.5 text-sm font-medium transition-colors ${
                activeTab === tab ? 'text-foreground' : 'text-foreground-muted hover:text-foreground'
              }`}
            >
              {activeTab === tab && (
                <motion.div
                  layoutId="discoverTab"
                  className="absolute inset-0 rounded-md bg-surface-hover"
                  transition={{ type: 'spring', bounce: 0.2, duration: 0.4 }}
                />
              )}
              <span className="relative z-10 capitalize">{tab}</span>
            </button>
          ))}
        </motion.div>

        <motion.div
          initial={{ opacity: 0, y: 10 }}
          animate={{ opacity: 1, y: 0 }}
          transition={{ delay: 0.15 }}
          className="flex flex-col gap-3 sm:flex-row sm:items-end"
        >
          <div className="flex-1">
            <Input
              id="search"
              placeholder={activeTab === 'players' ? 'Search players...' : 'Search games...'}
              value={searchQuery}
              onChange={(e) => handleSearchChange(e.target.value)}
            />
          </div>
          {activeTab === 'players' && (
            <>
              <FilterPills label="Region" options={regions} selected={selectedRegion} onSelect={(v) => { setSelectedRegion(v); setPlayerPage(1); }} />
              <FilterPills label="Level" options={levels} selected={selectedLevel} onSelect={(v) => { setSelectedLevel(v); setPlayerPage(1); }} />
              <button
                onClick={() => { setOnlyLFT(!onlyLFT); setPlayerPage(1); }}
                className={`shrink-0 rounded-lg border px-3 py-2 text-sm font-medium transition-colors ${
                  onlyLFT
                    ? 'border-accent bg-accent/10 text-accent'
                    : 'border-border text-foreground-muted hover:border-border-hover'
                }`}
              >
                LFT Only
              </button>
            </>
          )}
        </motion.div>

        <AnimatePresence mode="wait">
          {activeTab === 'games' ? (
            <motion.div key="games" initial={{ opacity: 0 }} animate={{ opacity: 1 }} exit={{ opacity: 0 }}>
              {gamesLoading ? (
                <div className="flex justify-center py-16">
                  <motion.div
                    animate={{ rotate: 360 }}
                    transition={{ duration: 1, repeat: Infinity, ease: 'linear' }}
                    className="h-8 w-8 rounded-full border-2 border-primary border-t-transparent"
                  />
                </div>
              ) : (
                <>
                  <motion.div
                    variants={staggerContainer}
                    initial="hidden"
                    animate="show"
                    className="grid gap-4 sm:grid-cols-2 lg:grid-cols-3 xl:grid-cols-4"
                  >
                    {gamesResult?.items.map((game) => (
                      <motion.div key={game.id} variants={staggerItem}>
                        <GameCard game={game} />
                      </motion.div>
                    ))}
                  </motion.div>

                  {gamesResult && gamesResult.items.length === 0 && (
                    <div className="flex flex-col items-center py-16 text-center">
                      <div className="text-4xl">🎮</div>
                      <p className="mt-3 text-lg font-semibold text-foreground">No games found</p>
                      <p className="mt-1 text-sm text-foreground-muted">Try a different search term</p>
                    </div>
                  )}

                  {gamesResult && gamesResult.totalCount > 0 && (
                    <Pagination
                      page={gamePage}
                      totalCount={gamesResult.totalCount}
                      pageSize={12}
                      hasNext={gamesResult.hasNextPage}
                      hasPrev={gamesResult.hasPreviousPage}
                      onPageChange={setGamePage}
                    />
                  )}
                </>
              )}
            </motion.div>
          ) : (
            <motion.div key="players" initial={{ opacity: 0 }} animate={{ opacity: 1 }} exit={{ opacity: 0 }}>
              {playersLoading ? (
                <div className="flex justify-center py-16">
                  <motion.div
                    animate={{ rotate: 360 }}
                    transition={{ duration: 1, repeat: Infinity, ease: 'linear' }}
                    className="h-8 w-8 rounded-full border-2 border-primary border-t-transparent"
                  />
                </div>
              ) : (
                <>
                  <motion.div
                    variants={staggerContainer}
                    initial="hidden"
                    animate="show"
                    className="grid gap-4 sm:grid-cols-2 lg:grid-cols-3"
                  >
                    {playersResult?.items.map((player) => (
                      <PlayerCard key={player.id} player={player} />
                    ))}
                  </motion.div>

                  {playersResult && playersResult.items.length === 0 && (
                    <div className="flex flex-col items-center py-16 text-center">
                      <div className="text-4xl">🔍</div>
                      <p className="mt-3 text-lg font-semibold text-foreground">No players found</p>
                      <p className="mt-1 text-sm text-foreground-muted">
                        {playersResult.totalCount === 0
                          ? 'No registered players yet. Be the first!'
                          : 'Try adjusting your filters'}
                      </p>
                    </div>
                  )}

                  {playersResult && playersResult.totalCount > 0 && (
                    <Pagination
                      page={playerPage}
                      totalCount={playersResult.totalCount}
                      pageSize={12}
                      hasNext={playersResult.hasNextPage}
                      hasPrev={playersResult.hasPreviousPage}
                      onPageChange={setPlayerPage}
                    />
                  )}
                </>
              )}
            </motion.div>
          )}
        </AnimatePresence>
      </div>
    </AnimatedPage>
  );
}

function Pagination({ page, totalCount, pageSize, hasNext, hasPrev, onPageChange }: {
  page: number;
  totalCount: number;
  pageSize: number;
  hasNext: boolean;
  hasPrev: boolean;
  onPageChange: (p: number) => void;
}) {
  const totalPages = Math.ceil(totalCount / pageSize);

  return (
    <motion.div
      initial={{ opacity: 0 }}
      animate={{ opacity: 1 }}
      className="mt-8 flex items-center justify-center gap-3"
    >
      <Button
        variant="outline"
        size="sm"
        disabled={!hasPrev}
        onClick={() => onPageChange(page - 1)}
      >
        ← Previous
      </Button>
      <span className="text-sm text-foreground-muted">
        Page {page} of {totalPages}
      </span>
      <Button
        variant="outline"
        size="sm"
        disabled={!hasNext}
        onClick={() => onPageChange(page + 1)}
      >
        Next →
      </Button>
    </motion.div>
  );
}

function FilterPills({ label, options, selected, onSelect }: {
  label: string;
  options: string[];
  selected: string;
  onSelect: (val: string) => void;
}) {
  const [open, setOpen] = useState(false);

  return (
    <div className="relative">
      <button
        onClick={() => setOpen(!open)}
        className="flex items-center gap-2 rounded-lg border border-border bg-surface px-3 py-2 text-sm text-foreground-muted transition-colors hover:border-border-hover"
      >
        <span className="text-foreground-subtle">{label}:</span>
        <span className="font-medium text-foreground">{selected}</span>
        <svg className="h-3 w-3" fill="none" stroke="currentColor" viewBox="0 0 24 24" strokeWidth={2}>
          <path strokeLinecap="round" strokeLinejoin="round" d="M19 9l-7 7-7-7" />
        </svg>
      </button>
      <AnimatePresence>
        {open && (
          <>
            <div className="fixed inset-0 z-30" onClick={() => setOpen(false)} />
            <motion.div
              initial={{ opacity: 0, y: -5, scale: 0.95 }}
              animate={{ opacity: 1, y: 0, scale: 1 }}
              exit={{ opacity: 0, y: -5, scale: 0.95 }}
              transition={{ duration: 0.15 }}
              className="absolute left-0 top-full z-40 mt-1 flex flex-wrap gap-1 rounded-lg border border-border bg-surface p-2 shadow-xl"
            >
              {options.map((opt) => (
                <button
                  key={opt}
                  onClick={() => { onSelect(opt); setOpen(false); }}
                  className={`rounded-md px-3 py-1.5 text-xs font-medium transition-colors ${
                    selected === opt
                      ? 'bg-primary text-primary-foreground'
                      : 'text-foreground-muted hover:bg-surface-hover'
                  }`}
                >
                  {opt}
                </button>
              ))}
            </motion.div>
          </>
        )}
      </AnimatePresence>
    </div>
  );
}

function PlayerCard({ player }: { player: DiscoverPlayerResponse }) {
  const levelColors: Record<string, 'default' | 'success' | 'primary' | 'warning' | 'accent'> = {
    Beginner: 'default',
    Intermediate: 'success',
    Advanced: 'primary',
    Expert: 'accent',
  };

  return (
    <motion.div variants={staggerItem}>
      <Link to={`/profile/${player.id}`}>
        <motion.div
          whileHover={{ y: -4, scale: 1.01 }}
          transition={{ duration: 0.2 }}
          className="group relative overflow-hidden rounded-xl border border-border bg-surface p-5 transition-colors hover:border-primary/30"
        >
          <div className="absolute inset-0 bg-gradient-to-br from-primary/5 to-transparent opacity-0 transition-opacity group-hover:opacity-100" />

          <div className="relative flex items-start gap-4">
            <motion.div whileHover={{ scale: 1.1 }} className="shrink-0">
              <UserAvatar username={player.username} avatarUrl={player.avatarUrl} size="lg" />
            </motion.div>
            <div className="min-w-0 flex-1">
              <div className="flex items-center gap-2">
                <h3 className="truncate font-semibold text-foreground">{player.username}</h3>
                {player.lookingForTeam && <Badge variant="accent">LFT</Badge>}
              </div>
              {player.bio && (
                <p className="mt-1 line-clamp-2 text-sm text-foreground-muted">{player.bio}</p>
              )}

              <div className="mt-3 flex flex-wrap gap-1.5">
                {player.experienceLevel && (
                  <Badge variant={levelColors[player.experienceLevel] ?? 'default'}>
                    {player.experienceLevel}
                  </Badge>
                )}
                {player.region && <Badge>{player.region}</Badge>}
                {player.communicationPreference && (
                  <Badge>{player.communicationPreference}</Badge>
                )}
              </div>

              {player.games.length > 0 && (
                <div className="mt-3 flex flex-wrap gap-1">
                  {player.games.slice(0, 3).map((game) => (
                    <span key={game.gameId} className="flex items-center gap-1 rounded-md bg-surface-hover px-2 py-0.5 text-xs text-foreground-muted">
                      {game.gameImageUrl && (
                        <img src={game.gameImageUrl} alt="" className="h-3 w-3 rounded-sm object-cover" />
                      )}
                      {game.gameName}
                    </span>
                  ))}
                  {player.games.length > 3 && (
                    <span className="rounded-md bg-surface-hover px-2 py-0.5 text-xs text-foreground-subtle">
                      +{player.games.length - 3}
                    </span>
                  )}
                </div>
              )}
            </div>
          </div>

          <div className="mt-4 flex justify-end">
            <Button variant="ghost" size="sm">View Profile →</Button>
          </div>
        </motion.div>
      </Link>
    </motion.div>
  );
}
