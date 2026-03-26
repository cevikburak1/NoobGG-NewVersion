import { useState, useRef } from 'react';
import { Link } from 'react-router-dom';
import { motion, AnimatePresence, useScroll, useTransform } from 'framer-motion';
import { useGameBrowse, useGameSearch } from '@/features/games/hooks';
import { useDiscoverPlayers } from '@/features/users/hooks';
import { useDebounce } from '@/hooks/useDebounce';
import { Button, Input, Badge, AnimatedPage, staggerContainer, staggerItem } from '@/components/ui';
import { UserAvatar } from '@/components/common/userAvatar';
import { GameCard } from '@/components/common/gameCard';
import type { DiscoverPlayerResponse } from '@/features/users/types';

const REGIONS = ['All', 'EU', 'NA', 'TR', 'AS', 'SA', 'OCE', 'ME', 'AF', 'CIS', 'SEA'] as const;
const LEVELS = ['All', 'Beginner', 'Intermediate', 'Advanced', 'Expert'] as const;
const GENRES = ['All', 'Action', 'RPG', 'Shooter', 'Strategy', 'MMORPG', 'Sports', 'Racing', 'Puzzle', 'Adventure'] as const;
const PLATFORMS = ['All', 'PC', 'PlayStation', 'Xbox', 'Nintendo', 'iOS', 'Android', 'Linux', 'macOS'] as const;

const LEVEL_META: Record<string, { color: 'default' | 'success' | 'primary' | 'warning' | 'accent'; icon: string }> = {
  Beginner: { color: 'default', icon: '🌱' },
  Intermediate: { color: 'success', icon: '⚡' },
  Advanced: { color: 'primary', icon: '🔥' },
  Expert: { color: 'accent', icon: '💎' },
};

export default function DiscoverPage() {
  const [searchQuery, setSearchQuery] = useState('');
  const [activeTab, setActiveTab] = useState<'players' | 'games'>('games');

  const [selectedRegion, setSelectedRegion] = useState('All');
  const [selectedLevel, setSelectedLevel] = useState('All');
  const [selectedGenre, setSelectedGenre] = useState('All');
  const [selectedPlatform, setSelectedPlatform] = useState('All');
  const [onlyMultiplayer, setOnlyMultiplayer] = useState(false);
  const [onlyCoop, setOnlyCoop] = useState(false);
  const [onlyPvp, setOnlyPvp] = useState(false);
  const [onlyF2P, setOnlyF2P] = useState(false);
  const [onlyLFT, setOnlyLFT] = useState(false);

  const [gameFilterSearch, setGameFilterSearch] = useState('');
  const [gameFilterId, setGameFilterId] = useState<string | undefined>();
  const [gameFilterName, setGameFilterName] = useState('');
  const [showGameDropdown, setShowGameDropdown] = useState(false);

  const [gamePage, setGamePage] = useState(1);
  const [playerPage, setPlayerPage] = useState(1);

  const debouncedSearch = useDebounce(searchQuery, 300);
  const debouncedGameFilter = useDebounce(gameFilterSearch, 300);
  const { data: gameFilterResults } = useGameSearch(debouncedGameFilter);

  const heroRef = useRef<HTMLDivElement>(null);
  const { scrollYProgress } = useScroll({ target: heroRef, offset: ['start start', 'end start'] });
  const heroOpacity = useTransform(scrollYProgress, [0, 1], [1, 0]);

  const { data: gamesResult, isLoading: gamesLoading } = useGameBrowse({
    search: debouncedSearch || undefined,
    genre: selectedGenre !== 'All' ? selectedGenre : undefined,
    platform: selectedPlatform !== 'All' ? selectedPlatform : undefined,
    multiplayer: onlyMultiplayer || undefined,
    coop: onlyCoop || undefined,
    pvp: onlyPvp || undefined,
    freeToPlay: onlyF2P || undefined,
    page: gamePage,
    pageSize: 12,
  });

  const { data: playersResult, isLoading: playersLoading } = useDiscoverPlayers({
    search: debouncedSearch || undefined,
    gameId: gameFilterId,
    region: selectedRegion !== 'All' ? selectedRegion : undefined,
    experienceLevel: selectedLevel !== 'All' ? selectedLevel : undefined,
    lookingForTeam: onlyLFT || undefined,
    page: playerPage,
    pageSize: 12,
  });

  const resetFilters = () => {
    setSearchQuery('');
    setSelectedRegion('All');
    setSelectedLevel('All');
    setSelectedGenre('All');
    setSelectedPlatform('All');
    setOnlyMultiplayer(false);
    setOnlyCoop(false);
    setOnlyPvp(false);
    setOnlyF2P(false);
    setOnlyLFT(false);
    setGameFilterId(undefined);
    setGameFilterName('');
    setGameFilterSearch('');
    setGamePage(1);
    setPlayerPage(1);
  };

  const handleSearchChange = (val: string) => {
    setSearchQuery(val);
    setGamePage(1);
    setPlayerPage(1);
  };

  const clearGameFilter = () => {
    setGameFilterId(undefined);
    setGameFilterName('');
    setGameFilterSearch('');
    setPlayerPage(1);
  };

  const hasActiveFilters =
    searchQuery ||
    selectedRegion !== 'All' ||
    selectedLevel !== 'All' ||
    selectedGenre !== 'All' ||
    selectedPlatform !== 'All' ||
    onlyMultiplayer ||
    onlyCoop ||
    onlyPvp ||
    onlyF2P ||
    onlyLFT ||
    gameFilterId;

  const totalGames = gamesResult?.totalCount ?? 0;
  const totalPlayers = playersResult?.totalCount ?? 0;

  return (
    <AnimatedPage>
      <div className="space-y-6">
        {/* Hero Section */}
        <div ref={heroRef} className="relative -mx-4 -mt-4 overflow-hidden lg:-mx-8">
          <motion.div style={{ opacity: heroOpacity }} className="absolute inset-0 bg-linear-to-br from-primary/8 via-accent/5 to-transparent" />
          <div className="relative px-4 pb-6 pt-8 lg:px-8">
            <div className="mx-auto max-w-4xl text-center">
              <motion.h1
                initial={{ opacity: 0, y: 20 }}
                animate={{ opacity: 1, y: 0 }}
                className="text-4xl font-extrabold tracking-tight text-foreground lg:text-5xl"
              >
                Discover
              </motion.h1>
              <motion.p
                initial={{ opacity: 0, y: 10 }}
                animate={{ opacity: 1, y: 0 }}
                transition={{ delay: 0.05 }}
                className="mx-auto mt-3 max-w-lg text-foreground-muted"
              >
                Find players and games that match your style
              </motion.p>

              {/* Search Bar */}
              <motion.div
                initial={{ opacity: 0, y: 10 }}
                animate={{ opacity: 1, y: 0 }}
                transition={{ delay: 0.1 }}
                className="mx-auto mt-6 max-w-xl"
              >
                <div className="relative">
                  <svg className="pointer-events-none absolute left-3.5 top-1/2 h-4 w-4 -translate-y-1/2 text-foreground-subtle" fill="none" stroke="currentColor" viewBox="0 0 24 24" strokeWidth={2}>
                    <path strokeLinecap="round" strokeLinejoin="round" d="M21 21l-5.197-5.197m0 0A7.5 7.5 0 105.196 5.196a7.5 7.5 0 0010.607 10.607z" />
                  </svg>
                  <input
                    placeholder={activeTab === 'players' ? 'Search players by username...' : 'Search games...'}
                    value={searchQuery}
                    onChange={(e) => handleSearchChange(e.target.value)}
                    className="w-full rounded-xl border border-border bg-surface/80 py-3 pl-10 pr-4 text-sm text-foreground placeholder:text-foreground-subtle backdrop-blur-sm transition-colors focus:border-primary/50 focus:outline-none focus:ring-2 focus:ring-primary/20"
                  />
                </div>
              </motion.div>

              {/* Stats */}
              <motion.div
                initial={{ opacity: 0, y: 10 }}
                animate={{ opacity: 1, y: 0 }}
                transition={{ delay: 0.15 }}
                className="mt-5 flex items-center justify-center gap-6"
              >
                <StatPill icon="🎮" label="Games" value={totalGames} />
                <StatPill icon="👥" label="Players" value={totalPlayers} />
              </motion.div>
            </div>
          </div>
        </div>

        {/* Tabs */}
        <motion.div
          initial={{ opacity: 0, y: 10 }}
          animate={{ opacity: 1, y: 0 }}
          transition={{ delay: 0.15 }}
          className="flex items-center gap-3"
        >
          <div className="flex gap-1 rounded-xl border border-border bg-surface p-1">
            {([
              { key: 'games' as const, label: 'Games', icon: '🎮' },
              { key: 'players' as const, label: 'Players', icon: '👥' },
            ]).map((tab) => (
              <button
                key={tab.key}
                onClick={() => setActiveTab(tab.key)}
                className={`relative flex items-center gap-2 rounded-lg px-5 py-2.5 text-sm font-medium transition-colors ${
                  activeTab === tab.key ? 'text-foreground' : 'text-foreground-muted hover:text-foreground'
                }`}
              >
                {activeTab === tab.key && (
                  <motion.div
                    layoutId="discoverTab"
                    className="absolute inset-0 rounded-lg bg-surface-hover"
                    transition={{ type: 'spring', bounce: 0.2, duration: 0.4 }}
                  />
                )}
                <span className="relative z-10">{tab.icon}</span>
                <span className="relative z-10">{tab.label}</span>
              </button>
            ))}
          </div>

          {hasActiveFilters && (
            <motion.button
              initial={{ opacity: 0, scale: 0.9 }}
              animate={{ opacity: 1, scale: 1 }}
              onClick={resetFilters}
              className="flex items-center gap-1.5 rounded-lg border border-danger/30 bg-danger/5 px-3 py-2 text-xs font-medium text-danger transition-colors hover:bg-danger/10"
            >
              <svg className="h-3 w-3" fill="none" stroke="currentColor" viewBox="0 0 24 24" strokeWidth={2.5}>
                <path strokeLinecap="round" strokeLinejoin="round" d="M6 18L18 6M6 6l12 12" />
              </svg>
              Clear filters
            </motion.button>
          )}
        </motion.div>

        {/* Filter Bar */}
        <motion.div
          initial={{ opacity: 0, y: 10 }}
          animate={{ opacity: 1, y: 0 }}
          transition={{ delay: 0.2 }}
          className="relative z-20 rounded-xl border border-border/50 bg-surface/60 p-4 backdrop-blur-sm"
        >
          <AnimatePresence mode="wait">
            {activeTab === 'games' ? (
              <motion.div key="gameFilters" initial={{ opacity: 0 }} animate={{ opacity: 1 }} exit={{ opacity: 0 }} className="flex flex-wrap items-center gap-3">
                <span className="text-xs font-semibold uppercase tracking-wider text-foreground-subtle">Filters</span>
                <div className="h-4 w-px bg-border" />
                <FilterPills label="Genre" options={[...GENRES]} selected={selectedGenre} onSelect={(v) => { setSelectedGenre(v); setGamePage(1); }} />
                <FilterPills label="Platform" options={[...PLATFORMS]} selected={selectedPlatform} onSelect={(v) => { setSelectedPlatform(v); setGamePage(1); }} />
                <div className="h-4 w-px bg-border" />
                <ToggleChip label="👥 Multiplayer" active={onlyMultiplayer} onToggle={() => { setOnlyMultiplayer(!onlyMultiplayer); setGamePage(1); }} />
                <ToggleChip label="🤝 Co-op" active={onlyCoop} onToggle={() => { setOnlyCoop(!onlyCoop); setGamePage(1); }} />
                <ToggleChip label="⚔️ PvP" active={onlyPvp} onToggle={() => { setOnlyPvp(!onlyPvp); setGamePage(1); }} />
                <ToggleChip label="🆓 F2P" active={onlyF2P} onToggle={() => { setOnlyF2P(!onlyF2P); setGamePage(1); }} />
              </motion.div>
            ) : (
              <motion.div key="playerFilters" initial={{ opacity: 0 }} animate={{ opacity: 1 }} exit={{ opacity: 0 }} className="flex flex-wrap items-center gap-3">
                <span className="text-xs font-semibold uppercase tracking-wider text-foreground-subtle">Filters</span>
                <div className="h-4 w-px bg-border" />

                {/* Game filter for players */}
                <div className="relative">
                  {gameFilterId ? (
                    <div className="flex h-[34px] items-center gap-2 rounded-lg border border-primary/40 bg-primary/5 px-3">
                      <span className="text-xs">🎮</span>
                      <span className="max-w-[100px] truncate text-xs font-medium text-foreground">{gameFilterName}</span>
                      <button onClick={clearGameFilter} className="text-foreground-muted hover:text-danger transition-colors">
                        <svg className="h-3 w-3" fill="none" stroke="currentColor" viewBox="0 0 24 24" strokeWidth={2.5}>
                          <path strokeLinecap="round" strokeLinejoin="round" d="M6 18L18 6M6 6l12 12" />
                        </svg>
                      </button>
                    </div>
                  ) : (
                    <>
                      <input
                        placeholder="Game..."
                        value={gameFilterSearch}
                        onChange={(e) => { setGameFilterSearch(e.target.value); setShowGameDropdown(true); }}
                        onFocus={() => setShowGameDropdown(true)}
                        className="h-[34px] w-32 rounded-lg border border-border bg-surface px-3 text-xs text-foreground placeholder:text-foreground-subtle transition-colors focus:border-primary/50 focus:outline-none"
                      />
                      <AnimatePresence>
                        {showGameDropdown && gameFilterResults && gameFilterResults.length > 0 && (
                          <>
                            <div className="fixed inset-0 z-30" onClick={() => setShowGameDropdown(false)} />
                            <motion.div
                              initial={{ opacity: 0, y: -4 }}
                              animate={{ opacity: 1, y: 0 }}
                              exit={{ opacity: 0, y: -4 }}
                              className="absolute left-0 top-full z-40 mt-1 max-h-48 w-64 overflow-y-auto rounded-lg border border-border bg-surface shadow-xl"
                            >
                              {gameFilterResults.map((g) => (
                                <button
                                  key={g.id}
                                  className="flex w-full items-center gap-2.5 px-3 py-2 text-left text-xs text-foreground hover:bg-surface-hover transition-colors"
                                  onClick={() => {
                                    setGameFilterId(g.id);
                                    setGameFilterName(g.name);
                                    setGameFilterSearch('');
                                    setShowGameDropdown(false);
                                    setPlayerPage(1);
                                  }}
                                >
                                  {g.backgroundImageUrl ? (
                                    <img src={g.backgroundImageUrl} alt={g.name} className="h-6 w-9 shrink-0 rounded object-cover" />
                                  ) : (
                                    <div className="flex h-6 w-9 shrink-0 items-center justify-center rounded bg-surface-hover text-[10px]">🎮</div>
                                  )}
                                  <span className="min-w-0 flex-1 truncate font-medium">{g.name}</span>
                                </button>
                              ))}
                            </motion.div>
                          </>
                        )}
                      </AnimatePresence>
                    </>
                  )}
                </div>

                <FilterPills label="Region" options={[...REGIONS]} selected={selectedRegion} onSelect={(v) => { setSelectedRegion(v); setPlayerPage(1); }} />
                <FilterPills label="Level" options={[...LEVELS]} selected={selectedLevel} onSelect={(v) => { setSelectedLevel(v); setPlayerPage(1); }} />

                <button
                  onClick={() => { setOnlyLFT(!onlyLFT); setPlayerPage(1); }}
                  className={`flex h-[34px] items-center gap-1.5 rounded-lg border px-3 text-xs font-medium transition-colors ${
                    onlyLFT
                      ? 'border-accent bg-accent/10 text-accent'
                      : 'border-border text-foreground-muted hover:border-border-hover'
                  }`}
                >
                  <span className="text-[10px]">🎯</span>
                  LFT Only
                </button>
              </motion.div>
            )}
          </AnimatePresence>
        </motion.div>

        {/* Content */}
        <AnimatePresence mode="wait">
          {activeTab === 'games' ? (
            <motion.div key="games" initial={{ opacity: 0 }} animate={{ opacity: 1 }} exit={{ opacity: 0 }}>
              {gamesLoading ? (
                <LoadingSpinner />
              ) : gamesResult && gamesResult.items.length > 0 ? (
                <>
                  <motion.div
                    variants={staggerContainer}
                    initial="hidden"
                    animate="show"
                    className="grid gap-4 sm:grid-cols-2 lg:grid-cols-3 xl:grid-cols-4"
                  >
                    {gamesResult.items.map((game) => (
                      <motion.div key={game.id} variants={staggerItem}>
                        <GameCard game={game} />
                      </motion.div>
                    ))}
                  </motion.div>
                  <Pagination
                    page={gamePage}
                    totalCount={gamesResult.totalCount}
                    pageSize={12}
                    hasNext={gamesResult.hasNextPage}
                    hasPrev={gamesResult.hasPreviousPage}
                    onPageChange={setGamePage}
                  />
                </>
              ) : (
                <EmptyState icon="🎮" title="No games found" subtitle="Try a different search term or genre" />
              )}
            </motion.div>
          ) : (
            <motion.div key="players" initial={{ opacity: 0 }} animate={{ opacity: 1 }} exit={{ opacity: 0 }}>
              {playersLoading ? (
                <LoadingSpinner />
              ) : playersResult && playersResult.items.length > 0 ? (
                <>
                  <motion.div
                    variants={staggerContainer}
                    initial="hidden"
                    animate="show"
                    className="grid gap-4 sm:grid-cols-2 lg:grid-cols-3"
                  >
                    {playersResult.items.map((player) => (
                      <PlayerCard key={player.id} player={player} />
                    ))}
                  </motion.div>
                  <Pagination
                    page={playerPage}
                    totalCount={playersResult.totalCount}
                    pageSize={12}
                    hasNext={playersResult.hasNextPage}
                    hasPrev={playersResult.hasPreviousPage}
                    onPageChange={setPlayerPage}
                  />
                </>
              ) : (
                <EmptyState
                  icon="🔍"
                  title="No players found"
                  subtitle={playersResult?.totalCount === 0 ? 'No registered players yet' : 'Try adjusting your filters'}
                />
              )}
            </motion.div>
          )}
        </AnimatePresence>
      </div>
    </AnimatedPage>
  );
}

function StatPill({ icon, label, value }: { icon: string; label: string; value: number }) {
  return (
    <div className="flex items-center gap-2 rounded-full border border-border/50 bg-surface/60 px-4 py-1.5 backdrop-blur-sm">
      <span className="text-sm">{icon}</span>
      <span className="text-xs text-foreground-muted">{label}</span>
      <span className="text-sm font-bold text-foreground">{value.toLocaleString()}</span>
    </div>
  );
}

function LoadingSpinner() {
  return (
    <div className="flex justify-center py-20">
      <motion.div
        animate={{ rotate: 360 }}
        transition={{ duration: 1, repeat: Infinity, ease: 'linear' }}
        className="h-8 w-8 rounded-full border-2 border-primary border-t-transparent"
      />
    </div>
  );
}

function EmptyState({ icon, title, subtitle }: { icon: string; title: string; subtitle: string }) {
  return (
    <motion.div
      initial={{ opacity: 0, scale: 0.95 }}
      animate={{ opacity: 1, scale: 1 }}
      className="flex flex-col items-center py-20 text-center"
    >
      <motion.div animate={{ y: [0, -8, 0] }} transition={{ duration: 2, repeat: Infinity }} className="text-5xl">
        {icon}
      </motion.div>
      <h3 className="mt-4 text-lg font-bold text-foreground">{title}</h3>
      <p className="mt-1.5 text-sm text-foreground-muted">{subtitle}</p>
    </motion.div>
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
  if (!hasNext && !hasPrev) return null;
  const totalPages = Math.ceil(totalCount / pageSize);

  return (
    <motion.div
      initial={{ opacity: 0 }}
      animate={{ opacity: 1 }}
      className="mt-8 flex items-center justify-center gap-3"
    >
      <Button variant="outline" size="sm" disabled={!hasPrev} onClick={() => onPageChange(page - 1)}>
        ← Previous
      </Button>
      <span className="rounded-lg border border-border bg-surface px-3 py-1.5 text-sm font-medium text-foreground">
        {page}
      </span>
      <span className="text-xs text-foreground-subtle">of {totalPages}</span>
      <Button variant="outline" size="sm" disabled={!hasNext} onClick={() => onPageChange(page + 1)}>
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
        className={`flex h-[34px] items-center gap-2 rounded-lg border px-3 text-xs transition-colors ${
          selected !== 'All'
            ? 'border-primary/40 bg-primary/5 text-primary'
            : 'border-border bg-surface text-foreground-muted hover:border-border-hover'
        }`}
      >
        <span className="text-foreground-subtle">{label}:</span>
        <span className="font-medium">{selected}</span>
        <svg className={`h-3 w-3 transition-transform ${open ? 'rotate-180' : ''}`} fill="none" stroke="currentColor" viewBox="0 0 24 24" strokeWidth={2}>
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
              style={{ minWidth: '160px' }}
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

function ToggleChip({ label, active, onToggle }: { label: string; active: boolean; onToggle: () => void }) {
  return (
    <button
      onClick={onToggle}
      className={`flex h-[34px] items-center gap-1 rounded-lg border px-3 text-xs font-medium transition-colors ${
        active
          ? 'border-primary/40 bg-primary/10 text-primary'
          : 'border-border text-foreground-muted hover:border-border-hover'
      }`}
    >
      {label}
    </button>
  );
}

function PlayerCard({ player }: { player: DiscoverPlayerResponse }) {
  const meta = LEVEL_META[player.experienceLevel ?? ''];

  return (
    <motion.div variants={staggerItem}>
      <Link to={`/profile/${player.id}`}>
        <motion.div
          whileHover={{ y: -4, scale: 1.01 }}
          transition={{ duration: 0.2 }}
          className="group relative overflow-hidden rounded-xl border border-border bg-surface transition-colors hover:border-primary/30"
        >
          {/* Card Header with gradient */}
          <div className="relative h-20 overflow-hidden bg-linear-to-br from-primary/15 via-accent/10 to-surface">
            <div className="absolute inset-0 bg-linear-to-b from-transparent to-surface" />
            {player.lookingForTeam && (
              <motion.div
                initial={{ opacity: 0, x: 10 }}
                animate={{ opacity: 1, x: 0 }}
                className="absolute right-3 top-3"
              >
                <Badge variant="accent" className="border border-accent/30 shadow-sm">🎯 LFT</Badge>
              </motion.div>
            )}
          </div>

          {/* Avatar */}
          <div className="relative -mt-8 px-5">
            <motion.div whileHover={{ scale: 1.1 }} className="inline-block rounded-full border-2 border-surface p-0.5">
              <UserAvatar username={player.username} avatarUrl={player.avatarUrl} size="lg" />
            </motion.div>
          </div>

          {/* Content */}
          <div className="px-5 pb-5 pt-2">
            <h3 className="text-base font-bold text-foreground">{player.username}</h3>

            {player.bio && (
              <p className="mt-1 line-clamp-2 text-xs leading-relaxed text-foreground-muted">{player.bio}</p>
            )}

            {/* Info Badges */}
            <div className="mt-3 flex flex-wrap gap-1.5">
              {player.experienceLevel && meta && (
                <Badge variant={meta.color}>
                  {meta.icon} {player.experienceLevel}
                </Badge>
              )}
              {player.region && <Badge>{player.region}</Badge>}
              {player.communicationPreference && (
                <Badge variant="default">
                  {player.communicationPreference === 'Voice' ? '🎙️' : player.communicationPreference === 'Text' ? '💬' : '🔊'}{' '}
                  {player.communicationPreference}
                </Badge>
              )}
            </div>

            {/* Games Section */}
            {player.games.length > 0 && (
              <div className="mt-3 space-y-1.5">
                {player.games.slice(0, 3).map((game) => (
                  <div key={game.gameId} className="flex items-center gap-2 rounded-lg bg-surface-hover/60 px-2.5 py-1.5">
                    {game.gameImageUrl ? (
                      <img src={game.gameImageUrl} alt="" className="h-5 w-7 shrink-0 rounded object-cover" />
                    ) : (
                      <div className="flex h-5 w-7 shrink-0 items-center justify-center rounded bg-surface-active text-[9px]">🎮</div>
                    )}
                    <span className="min-w-0 flex-1 truncate text-xs font-medium text-foreground">{game.gameName}</span>
                    {game.rank && (
                      <span className="shrink-0 rounded bg-primary/10 px-1.5 py-0.5 text-[10px] font-bold text-primary">
                        {game.rank}
                      </span>
                    )}
                  </div>
                ))}
                {player.games.length > 3 && (
                  <span className="pl-2 text-[11px] text-foreground-subtle">+{player.games.length - 3} more games</span>
                )}
              </div>
            )}

            {/* Footer */}
            <div className="mt-4 flex justify-end">
              <span className="text-xs font-medium text-primary opacity-0 transition-opacity group-hover:opacity-100">
                View Profile →
              </span>
            </div>
          </div>
        </motion.div>
      </Link>
    </motion.div>
  );
}
