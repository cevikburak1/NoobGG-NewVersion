import { useParams, Link } from 'react-router-dom';
import { motion, useScroll, useTransform } from 'framer-motion';
import { useRef, useState } from 'react';
import { useGameDetail } from '@/features/games/hooks';
import { Button, Badge, AnimatedPage, Spinner } from '@/components/ui';
import { CommunityFeed } from '@/components/community/communityFeed';
import { GuideList } from '@/components/guides/guideList';
import { useTournaments } from '@/features/tournaments/hooks';
import type { TournamentListItemResponse } from '@/features/tournaments/types';

const PLATFORM_META: Record<string, { icon: string; color: string }> = {
  PC: { icon: '🖥️', color: 'from-blue-500/20 to-blue-600/10' },
  PlayStation: { icon: '🎮', color: 'from-blue-600/20 to-indigo-600/10' },
  Xbox: { icon: '🟢', color: 'from-green-500/20 to-green-600/10' },
  Nintendo: { icon: '🔴', color: 'from-red-500/20 to-red-600/10' },
  iOS: { icon: '📱', color: 'from-gray-400/20 to-gray-500/10' },
  Android: { icon: '🤖', color: 'from-green-400/20 to-green-500/10' },
  Linux: { icon: '🐧', color: 'from-yellow-500/20 to-yellow-600/10' },
  macOS: { icon: '🍎', color: 'from-gray-400/20 to-gray-500/10' },
};

const GAME_MODE_META: { key: string; label: string; icon: string; color: string }[] = [
  { key: 'isMultiplayer', label: 'Multiplayer', icon: '👥', color: 'from-primary/20 to-primary/5' },
  { key: 'isCoop', label: 'Co-op', icon: '🤝', color: 'from-success/20 to-success/5' },
  { key: 'isPvp', label: 'PvP', icon: '⚔️', color: 'from-danger/20 to-danger/5' },
  { key: 'isFreeToPlay', label: 'Free to Play', icon: '🆓', color: 'from-accent/20 to-accent/5' },
];

function RatingRing({ value, max, label, color }: { value: number; max: number; label: string; color: string }) {
  const pct = (value / max) * 100;
  const r = 40;
  const circ = 2 * Math.PI * r;
  const offset = circ - (pct / 100) * circ;

  return (
    <motion.div
      initial={{ opacity: 0, scale: 0.8 }}
      animate={{ opacity: 1, scale: 1 }}
      transition={{ delay: 0.4, type: 'spring', bounce: 0.3 }}
      className="flex flex-col items-center gap-2"
    >
      <div className="relative h-24 w-24">
        <svg className="h-full w-full -rotate-90" viewBox="0 0 100 100">
          <circle cx="50" cy="50" r={r} fill="none" stroke="currentColor" strokeWidth="6" className="text-surface-hover" />
          <motion.circle
            cx="50" cy="50" r={r}
            fill="none" stroke={color} strokeWidth="6"
            strokeLinecap="round"
            strokeDasharray={circ}
            initial={{ strokeDashoffset: circ }}
            animate={{ strokeDashoffset: offset }}
            transition={{ duration: 1.2, ease: 'easeOut', delay: 0.6 }}
          />
        </svg>
        <div className="absolute inset-0 flex items-center justify-center">
          <motion.span
            initial={{ opacity: 0 }}
            animate={{ opacity: 1 }}
            transition={{ delay: 0.8 }}
            className="text-xl font-bold text-foreground"
          >
            {max === 5 ? value.toFixed(1) : value}
          </motion.span>
        </div>
      </div>
      <span className="text-xs font-medium uppercase tracking-wider text-foreground-subtle">{label}</span>
    </motion.div>
  );
}

function StatCard({ icon, label, value, delay }: { icon: string; label: string; value: string; delay: number }) {
  return (
    <motion.div
      initial={{ opacity: 0, y: 16 }}
      animate={{ opacity: 1, y: 0 }}
      transition={{ delay }}
      className="flex items-center gap-3 rounded-xl border border-border/50 bg-surface/60 px-4 py-3 backdrop-blur-sm"
    >
      <span className="text-lg">{icon}</span>
      <div className="min-w-0">
        <div className="text-[11px] font-medium uppercase tracking-wider text-foreground-subtle">{label}</div>
        <div className="truncate text-sm font-semibold text-foreground">{value}</div>
      </div>
    </motion.div>
  );
}

const COMMUNITY_TABS = ['Feed', 'Guides', 'Tournaments'] as const;

function MiniTournamentCard({ t }: { t: TournamentListItemResponse }) {
  const statusColors: Record<string, string> = {
    Registration: 'bg-accent/20 text-accent',
    InProgress: 'bg-primary/20 text-primary',
    Completed: 'bg-success/20 text-success',
  };
  return (
    <Link to={`/tournaments/${t.id}`}>
      <motion.div
        whileHover={{ scale: 1.02, y: -2 }}
        className="rounded-xl border border-border/50 bg-surface/60 p-4 backdrop-blur-sm transition-shadow hover:shadow-lg"
      >
        <div className="flex items-center justify-between gap-2">
          <h4 className="truncate text-sm font-bold text-foreground">{t.name}</h4>
          <span className={`shrink-0 rounded-full px-2 py-0.5 text-[10px] font-semibold ${statusColors[t.status] ?? 'bg-surface-hover text-foreground-muted'}`}>
            {t.status}
          </span>
        </div>
        <div className="mt-2 flex items-center gap-3 text-xs text-foreground-muted">
          <span>{t.format}</span>
          <span>{t.currentParticipants}/{t.maxParticipants} players</span>
        </div>
        {t.prizeBadges.length > 0 && (
          <div className="mt-2 flex flex-wrap gap-1">
            {t.prizeBadges.slice(0, 3).map((b) => (
              <span key={b} className="rounded bg-yellow-500/10 px-1.5 py-0.5 text-[10px] font-medium text-yellow-400">{b}</span>
            ))}
          </div>
        )}
      </motion.div>
    </Link>
  );
}

function GameCommunityTabs({ gameId, gameName, gameSlug }: { gameId: string; gameName: string; gameSlug: string }) {
  const [activeTab, setActiveTab] = useState<(typeof COMMUNITY_TABS)[number]>('Feed');
  const { data: tournamentsData } = useTournaments({ gameId, page: 1 });

  return (
    <motion.div
      initial={{ opacity: 0, y: 20 }}
      animate={{ opacity: 1, y: 0 }}
      transition={{ delay: 0.55 }}
      className="mt-10"
    >
      <div className="mb-6 flex items-center gap-4">
        <div className="flex-1">
          <div className="flex items-center gap-4">
            <h2 className="text-lg font-bold text-foreground">Community Hub</h2>
            <Link to={`/community/boards/${gameSlug}`}>
              <Button variant="outline" size="sm">Open Full Forum</Button>
            </Link>
          </div>
          <p className="mt-2 text-sm text-foreground-muted">
            Quick discussions stay here. For full threads, topic browsing, and forum-style navigation, jump into the board.
          </p>
        </div>
        <div className="flex gap-1 rounded-lg border border-border bg-surface p-1">
          {COMMUNITY_TABS.map((tab) => (
            <button
              key={tab}
              onClick={() => setActiveTab(tab)}
              className={`relative rounded-md px-4 py-2 text-sm font-medium transition-colors ${
                activeTab === tab ? 'text-foreground' : 'text-foreground-muted hover:text-foreground'
              }`}
            >
              {activeTab === tab && (
                <motion.div
                  layoutId="communityTab"
                  className="absolute inset-0 rounded-md bg-surface-hover"
                  transition={{ type: 'spring', bounce: 0.2, duration: 0.4 }}
                />
              )}
              <span className="relative z-10">{tab}</span>
            </button>
          ))}
        </div>
      </div>

      {activeTab === 'Feed' && <CommunityFeed gameId={gameId} />}

      {activeTab === 'Guides' && <GuideList gameId={gameId} />}

      {activeTab === 'Tournaments' && (
        <div className="space-y-4">
          <div className="flex items-center justify-between">
            <p className="text-sm text-foreground-muted">
              Tournaments for <span className="font-semibold text-foreground">{gameName}</span>
            </p>
            <Link to="/tournaments">
              <Button variant="outline" size="sm">View All Tournaments</Button>
            </Link>
          </div>
          {tournamentsData?.tournaments && tournamentsData.tournaments.length > 0 ? (
            <div className="grid gap-3 sm:grid-cols-2 lg:grid-cols-3">
              {tournamentsData.tournaments.slice(0, 6).map((t, i) => (
                <motion.div
                  key={t.id}
                  initial={{ opacity: 0, y: 12 }}
                  animate={{ opacity: 1, y: 0 }}
                  transition={{ delay: 0.1 * i }}
                >
                  <MiniTournamentCard t={t} />
                </motion.div>
              ))}
            </div>
          ) : (
            <div className="flex flex-col items-center rounded-2xl border border-border/50 bg-surface/40 py-12 text-center backdrop-blur-sm">
              <span className="text-4xl">🏆</span>
              <p className="mt-3 text-sm font-medium text-foreground-muted">No tournaments yet for this game</p>
              <Link to="/tournaments" className="mt-3">
                <Button size="sm">Browse Tournaments</Button>
              </Link>
            </div>
          )}
        </div>
      )}
    </motion.div>
  );
}

export default function GameDetailPage() {
  const { gameId } = useParams<{ gameId: string }>();
  const { data: game, isLoading, error } = useGameDetail(gameId);
  const heroRef = useRef<HTMLDivElement>(null);
  const { scrollYProgress } = useScroll({ target: heroRef, offset: ['start start', 'end start'] });
  const heroY = useTransform(scrollYProgress, [0, 1], ['0%', '30%']);
  const heroOpacity = useTransform(scrollYProgress, [0, 0.7], [1, 0]);

  if (isLoading) {
    return (
      <div className="flex min-h-[60vh] items-center justify-center">
        <Spinner size="lg" />
      </div>
    );
  }

  if (error || !game) {
    return (
      <AnimatedPage>
        <div className="flex flex-col items-center py-32 text-center">
          <motion.div initial={{ scale: 0 }} animate={{ scale: 1 }} transition={{ type: 'spring', bounce: 0.5 }} className="text-7xl">
            🎮
          </motion.div>
          <h2 className="mt-6 text-2xl font-bold text-foreground">Game not found</h2>
          <p className="mt-2 text-foreground-muted">This game may have been removed or doesn't exist.</p>
          <Link to="/discover" className="mt-8">
            <Button size="lg">Browse Games</Button>
          </Link>
        </div>
      </AnimatedPage>
    );
  }

  const activeModes = GAME_MODE_META.filter((m) => game[m.key as keyof typeof game]);
  const metacriticColor = !game.metacritic ? '#888' : game.metacritic >= 75 ? '#22c55e' : game.metacritic >= 50 ? '#eab308' : '#ef4444';
  const releaseDate = game.releasedAt
    ? new Date(game.releasedAt).toLocaleDateString('en-US', { year: 'numeric', month: 'long', day: 'numeric' })
    : null;

  return (
    <AnimatedPage>
      <div className="relative -mx-4 -mt-4 lg:-mx-8">
        {/* Hero Section */}
        <div ref={heroRef} className="relative h-[420px] overflow-hidden lg:h-[480px]">
          {game.backgroundImageUrl ? (
            <motion.img
              src={game.backgroundImageUrl}
              alt=""
              style={{ y: heroY, opacity: heroOpacity }}
              className="absolute inset-0 h-full w-full object-cover"
            />
          ) : (
            <div className="absolute inset-0 bg-linear-to-br from-primary/20 via-surface to-background" />
          )}
          <div className="absolute inset-0 bg-linear-to-t from-background via-background/70 to-transparent" />
          <div className="absolute inset-0 bg-linear-to-r from-background/60 via-transparent to-background/60" />

          {/* Hero Content */}
          <div className="absolute inset-x-0 bottom-0 px-4 pb-8 lg:px-8">
            <div className="mx-auto max-w-6xl">
              <motion.div initial={{ opacity: 0, x: -20 }} animate={{ opacity: 1, x: 0 }} className="mb-4">
                <Link
                  to="/discover"
                  className="inline-flex items-center gap-1.5 rounded-full bg-surface/40 px-3 py-1.5 text-xs font-medium text-foreground-muted backdrop-blur-sm transition-colors hover:bg-surface/60 hover:text-foreground"
                >
                  <svg className="h-3.5 w-3.5" fill="none" stroke="currentColor" viewBox="0 0 24 24" strokeWidth={2.5}>
                    <path strokeLinecap="round" strokeLinejoin="round" d="M15.75 19.5L8.25 12l7.5-7.5" />
                  </svg>
                  Discover
                </Link>
              </motion.div>

              <motion.h1
                initial={{ opacity: 0, y: 20 }}
                animate={{ opacity: 1, y: 0 }}
                transition={{ delay: 0.1 }}
                className="text-4xl font-extrabold tracking-tight text-white drop-shadow-lg lg:text-5xl xl:text-6xl"
              >
                {game.name}
              </motion.h1>

              <motion.div
                initial={{ opacity: 0, y: 10 }}
                animate={{ opacity: 1, y: 0 }}
                transition={{ delay: 0.2 }}
                className="mt-4 flex flex-wrap items-center gap-2"
              >
                {game.genres.map((genre, i) => (
                  <motion.div key={genre} initial={{ opacity: 0, scale: 0.8 }} animate={{ opacity: 1, scale: 1 }} transition={{ delay: 0.25 + i * 0.04 }}>
                    <Badge variant="default" className="border border-border/40 bg-surface/50 px-3 py-1 backdrop-blur-sm">{genre}</Badge>
                  </motion.div>
                ))}
                {activeModes.map((mode, i) => (
                  <motion.div key={mode.key} initial={{ opacity: 0, scale: 0.8 }} animate={{ opacity: 1, scale: 1 }} transition={{ delay: 0.3 + i * 0.04 }}>
                    <Badge
                      variant={mode.key === 'isMultiplayer' ? 'primary' : mode.key === 'isCoop' ? 'success' : mode.key === 'isPvp' ? 'danger' : 'accent'}
                      className="px-3 py-1"
                    >
                      {mode.icon} {mode.label}
                    </Badge>
                  </motion.div>
                ))}
              </motion.div>
            </div>
          </div>
        </div>

        {/* Main Content */}
        <div className="mx-auto max-w-6xl px-4 lg:px-8">
          {/* Quick Stats Bar */}
          <motion.div
            initial={{ opacity: 0, y: 20 }}
            animate={{ opacity: 1, y: 0 }}
            transition={{ delay: 0.3 }}
            className="-mt-6 grid grid-cols-2 gap-3 sm:grid-cols-4"
          >
            {game.rating && (
              <StatCard icon="⭐" label="Rating" value={`${game.rating.toFixed(1)} / 5`} delay={0.35} />
            )}
            {game.metacritic && (
              <StatCard icon="🏆" label="Metacritic" value={String(game.metacritic)} delay={0.4} />
            )}
            {releaseDate && (
              <StatCard icon="📅" label="Released" value={releaseDate} delay={0.45} />
            )}
            {game.platforms.length > 0 && (
              <StatCard icon="🎮" label="Platforms" value={`${game.platforms.length} platforms`} delay={0.5} />
            )}
          </motion.div>

          <div className="mt-8 grid gap-8 lg:grid-cols-3">
            {/* Left Column - Main Content */}
            <div className="space-y-6 lg:col-span-2">
              {/* Ratings Section */}
              {(game.rating || game.metacritic) && (
                <motion.div
                  initial={{ opacity: 0, y: 20 }}
                  animate={{ opacity: 1, y: 0 }}
                  transition={{ delay: 0.35 }}
                  className="overflow-hidden rounded-2xl border border-border/50 bg-linear-to-br from-surface/80 to-surface/40 p-6 backdrop-blur-sm"
                >
                  <h2 className="mb-5 text-sm font-semibold uppercase tracking-widest text-foreground-subtle">Ratings & Scores</h2>
                  <div className="flex items-center justify-center gap-12">
                    {game.rating && (
                      <RatingRing value={game.rating} max={5} label="User Score" color="#7c3aed" />
                    )}
                    {game.metacritic && (
                      <RatingRing value={game.metacritic} max={100} label="Metacritic" color={metacriticColor} />
                    )}
                  </div>
                  {game.rating && (
                    <motion.div
                      initial={{ opacity: 0 }}
                      animate={{ opacity: 1 }}
                      transition={{ delay: 1 }}
                      className="mt-5 flex justify-center gap-1"
                    >
                      {[1, 2, 3, 4, 5].map((star) => (
                        <motion.span
                          key={star}
                          initial={{ opacity: 0, y: 10 }}
                          animate={{ opacity: 1, y: 0 }}
                          transition={{ delay: 1 + star * 0.08 }}
                          className={`text-xl ${star <= Math.round(game.rating!) ? 'text-yellow-400 drop-shadow-[0_0_6px_rgba(250,204,21,0.4)]' : 'text-foreground-subtle/30'}`}
                        >
                          ★
                        </motion.span>
                      ))}
                    </motion.div>
                  )}
                </motion.div>
              )}

              {/* About Section */}
              {game.description && (
                <motion.div
                  initial={{ opacity: 0, y: 20 }}
                  animate={{ opacity: 1, y: 0 }}
                  transition={{ delay: 0.4 }}
                  className="overflow-hidden rounded-2xl border border-border/50 bg-linear-to-br from-surface/80 to-surface/40 p-6 backdrop-blur-sm"
                >
                  <h2 className="mb-4 text-sm font-semibold uppercase tracking-widest text-foreground-subtle">About this Game</h2>
                  <p className="leading-relaxed text-foreground-muted">{game.description}</p>
                </motion.div>
              )}

              {/* Game Modes Section */}
              {activeModes.length > 0 && (
                <motion.div
                  initial={{ opacity: 0, y: 20 }}
                  animate={{ opacity: 1, y: 0 }}
                  transition={{ delay: 0.45 }}
                  className="overflow-hidden rounded-2xl border border-border/50 bg-linear-to-br from-surface/80 to-surface/40 p-6 backdrop-blur-sm"
                >
                  <h2 className="mb-5 text-sm font-semibold uppercase tracking-widest text-foreground-subtle">Game Modes</h2>
                  <div className="grid grid-cols-2 gap-3 sm:grid-cols-4">
                    {activeModes.map((mode, i) => (
                      <motion.div
                        key={mode.key}
                        initial={{ opacity: 0, y: 12 }}
                        animate={{ opacity: 1, y: 0 }}
                        transition={{ delay: 0.5 + i * 0.08 }}
                        whileHover={{ scale: 1.05, y: -2 }}
                        className={`flex flex-col items-center gap-2 rounded-xl bg-linear-to-b ${mode.color} border border-border/30 p-4 transition-shadow hover:shadow-lg`}
                      >
                        <span className="text-2xl">{mode.icon}</span>
                        <span className="text-xs font-semibold text-foreground">{mode.label}</span>
                      </motion.div>
                    ))}
                  </div>
                </motion.div>
              )}

              {/* Tags Section */}
              {game.tags.length > 0 && (
                <motion.div
                  initial={{ opacity: 0, y: 20 }}
                  animate={{ opacity: 1, y: 0 }}
                  transition={{ delay: 0.5 }}
                  className="overflow-hidden rounded-2xl border border-border/50 bg-linear-to-br from-surface/80 to-surface/40 p-6 backdrop-blur-sm"
                >
                  <h2 className="mb-4 text-sm font-semibold uppercase tracking-widest text-foreground-subtle">Tags</h2>
                  <div className="flex flex-wrap gap-2">
                    {game.tags.slice(0, 20).map((tag, i) => (
                      <motion.span
                        key={tag}
                        initial={{ opacity: 0, scale: 0.85 }}
                        animate={{ opacity: 1, scale: 1 }}
                        transition={{ delay: 0.55 + i * 0.02 }}
                        whileHover={{ scale: 1.08, backgroundColor: 'rgba(124,58,237,0.15)' }}
                        className="cursor-default rounded-full border border-border/30 bg-surface-hover/60 px-3 py-1 text-xs font-medium text-foreground-muted transition-colors hover:border-primary/30 hover:text-primary"
                      >
                        {tag}
                      </motion.span>
                    ))}
                    {game.tags.length > 20 && (
                      <span className="rounded-full bg-surface-hover/40 px-3 py-1 text-xs text-foreground-subtle">
                        +{game.tags.length - 20} more
                      </span>
                    )}
                  </div>
                </motion.div>
              )}
            </div>

            {/* Right Column - Sidebar */}
            <div className="space-y-4">
              {/* Platforms Card */}
              {game.platforms.length > 0 && (
                <motion.div
                  initial={{ opacity: 0, x: 20 }}
                  animate={{ opacity: 1, x: 0 }}
                  transition={{ delay: 0.35 }}
                  className="overflow-hidden rounded-2xl border border-border/50 bg-linear-to-br from-surface/80 to-surface/40 p-5 backdrop-blur-sm"
                >
                  <h3 className="mb-4 text-sm font-semibold uppercase tracking-widest text-foreground-subtle">Available On</h3>
                  <div className="space-y-2">
                    {game.platforms.map((platform, i) => {
                      const meta = PLATFORM_META[platform] || { icon: '🎮', color: 'from-surface-hover to-surface-hover/50' };
                      return (
                        <motion.div
                          key={platform}
                          initial={{ opacity: 0, x: 12 }}
                          animate={{ opacity: 1, x: 0 }}
                          transition={{ delay: 0.4 + i * 0.06 }}
                          className={`flex items-center gap-3 rounded-xl bg-linear-to-r ${meta.color} border border-border/20 px-4 py-2.5`}
                        >
                          <span className="text-base">{meta.icon}</span>
                          <span className="text-sm font-medium text-foreground">{platform}</span>
                        </motion.div>
                      );
                    })}
                  </div>
                </motion.div>
              )}

              {/* Release Info Card */}
              {releaseDate && (
                <motion.div
                  initial={{ opacity: 0, x: 20 }}
                  animate={{ opacity: 1, x: 0 }}
                  transition={{ delay: 0.4 }}
                  className="overflow-hidden rounded-2xl border border-border/50 bg-linear-to-br from-surface/80 to-surface/40 p-5 backdrop-blur-sm"
                >
                  <h3 className="mb-3 text-sm font-semibold uppercase tracking-widest text-foreground-subtle">Release Date</h3>
                  <div className="flex items-center gap-3">
                    <div className="flex h-10 w-10 items-center justify-center rounded-lg bg-primary/10 text-lg">📅</div>
                    <span className="font-semibold text-foreground">{releaseDate}</span>
                  </div>
                </motion.div>
              )}

              {/* Game Info Card */}
              <motion.div
                initial={{ opacity: 0, x: 20 }}
                animate={{ opacity: 1, x: 0 }}
                transition={{ delay: 0.45 }}
                className="overflow-hidden rounded-2xl border border-border/50 bg-linear-to-br from-surface/80 to-surface/40 p-5 backdrop-blur-sm"
              >
                <h3 className="mb-4 text-sm font-semibold uppercase tracking-widest text-foreground-subtle">Quick Info</h3>
                <div className="space-y-3">
                  <div className="flex items-center justify-between">
                    <span className="text-sm text-foreground-muted">Genres</span>
                    <span className="text-sm font-medium text-foreground">{game.genres.join(', ') || '—'}</span>
                  </div>
                  <div className="h-px bg-border/30" />
                  <div className="flex items-center justify-between">
                    <span className="text-sm text-foreground-muted">Tags</span>
                    <span className="text-sm font-medium text-foreground">{game.tags.length} tags</span>
                  </div>
                  <div className="h-px bg-border/30" />
                  <div className="flex items-center justify-between">
                    <span className="text-sm text-foreground-muted">Price</span>
                    <span className={`text-sm font-semibold ${game.isFreeToPlay ? 'text-accent' : 'text-foreground'}`}>
                      {game.isFreeToPlay ? 'Free to Play' : 'Paid'}
                    </span>
                  </div>
                  {game.rawgId && (
                    <>
                      <div className="h-px bg-border/30" />
                      <div className="flex items-center justify-between">
                        <span className="text-sm text-foreground-muted">RAWG ID</span>
                        <span className="text-sm font-medium text-foreground-subtle">#{game.rawgId}</span>
                      </div>
                    </>
                  )}
                </div>
              </motion.div>

              {/* CTA Card */}
              <motion.div
                initial={{ opacity: 0, x: 20 }}
                animate={{ opacity: 1, x: 0 }}
                transition={{ delay: 0.5 }}
                className="overflow-hidden rounded-2xl border border-primary/20 bg-linear-to-br from-primary/10 via-surface/80 to-surface/40 p-5 backdrop-blur-sm"
              >
                <div className="mb-3 flex items-center gap-2">
                  <span className="text-xl">🎯</span>
                  <h3 className="font-semibold text-foreground">Find Teammates</h3>
                </div>
                <p className="mb-4 text-sm leading-relaxed text-foreground-muted">
                  Looking for players to team up in {game.name}? Browse active rooms or create your own.
                </p>
                <div className="flex flex-col gap-2">
                  <Link to="/rooms">
                    <Button className="w-full" size="md">Browse Rooms</Button>
                  </Link>
                  <Link to="/rooms">
                    <Button variant="outline" className="w-full" size="md">Create Room</Button>
                  </Link>
                </div>
              </motion.div>
            </div>
          </div>

          {/* Community Hub Tabs */}
          <GameCommunityTabs gameId={game.id} gameName={game.name} gameSlug={game.slug} />

          {/* Bottom spacing */}
          <div className="h-12" />
        </div>
      </div>
    </AnimatedPage>
  );
}
