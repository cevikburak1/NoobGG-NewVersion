import { useState } from 'react';
import { Link } from 'react-router-dom';
import { motion } from 'framer-motion';
import { useRecommendedPlayers, useRecommendedRooms } from '@/features/recommendations/hooks';
import { useGameSearch } from '@/features/games/hooks';
import { useDebounce } from '@/hooks/useDebounce';
import { Badge, AnimatedPage, Spinner, staggerContainer, staggerItem } from '@/components/ui';
import { UserAvatar } from '@/components/common/userAvatar';
import { EmptyState } from '@/components/common/emptyState';
import type { RecommendedPlayerResponse, RecommendedRoomResponse } from '@/features/recommendations/types';

const LEVEL_META: Record<string, { color: 'default' | 'success' | 'primary' | 'warning' | 'accent'; icon: string }> = {
  Beginner: { color: 'default', icon: '🌱' },
  Intermediate: { color: 'success', icon: '⚡' },
  Advanced: { color: 'primary', icon: '🔥' },
  Expert: { color: 'accent', icon: '💎' },
};

export default function RecommendationsPage() {
  const [activeTab, setActiveTab] = useState<'players' | 'rooms'>('players');
  const [gameFilterSearch, setGameFilterSearch] = useState('');
  const [gameFilterId, setGameFilterId] = useState<string | undefined>();
  const [gameFilterName, setGameFilterName] = useState('');
  const [showGameDropdown, setShowGameDropdown] = useState(false);

  const debouncedGameFilter = useDebounce(gameFilterSearch, 300);
  const { data: gameFilterResults } = useGameSearch(debouncedGameFilter);

  const { data: players, isLoading: playersLoading } = useRecommendedPlayers(gameFilterId);
  const { data: rooms, isLoading: roomsLoading } = useRecommendedRooms(gameFilterId);

  return (
    <AnimatedPage>
      <div className="space-y-6">
        <div>
          <h1 className="text-2xl font-bold text-foreground">Recommendations</h1>
          <p className="mt-1 text-sm text-foreground-muted">
            Personalized suggestions based on your game profiles, region, and preferences
          </p>
        </div>

        <div className="flex flex-col gap-4 sm:flex-row sm:items-center sm:justify-between">
          <div className="flex gap-2">
            <TabButton
              label="Players"
              active={activeTab === 'players'}
              count={players?.length}
              onClick={() => setActiveTab('players')}
            />
            <TabButton
              label="Rooms"
              active={activeTab === 'rooms'}
              count={rooms?.length}
              onClick={() => setActiveTab('rooms')}
            />
          </div>

          <div className="relative w-full sm:w-64">
            <input
              type="text"
              placeholder="Filter by game..."
              value={gameFilterSearch}
              onChange={(e) => {
                setGameFilterSearch(e.target.value);
                setShowGameDropdown(true);
                if (!e.target.value) {
                  setGameFilterId(undefined);
                  setGameFilterName('');
                }
              }}
              className="w-full rounded-lg border border-border bg-surface px-3 py-2 text-sm text-foreground placeholder:text-foreground-subtle focus:border-primary focus:outline-none"
            />
            {gameFilterName && (
              <button
                onClick={() => {
                  setGameFilterSearch('');
                  setGameFilterId(undefined);
                  setGameFilterName('');
                }}
                className="absolute right-2 top-1/2 -translate-y-1/2 text-foreground-subtle hover:text-foreground"
              >
                ✕
              </button>
            )}
            {showGameDropdown && gameFilterResults && gameFilterResults.length > 0 && !gameFilterName && (
              <div className="absolute z-20 mt-1 max-h-48 w-full overflow-auto rounded-lg border border-border bg-surface shadow-lg">
                {gameFilterResults.map((game) => (
                  <button
                    key={game.id}
                    onClick={() => {
                      setGameFilterId(game.id);
                      setGameFilterName(game.name);
                      setGameFilterSearch(game.name);
                      setShowGameDropdown(false);
                    }}
                    className="flex w-full items-center gap-2 px-3 py-2 text-left text-sm hover:bg-surface-hover"
                  >
                    {game.backgroundImageUrl ? (
                      <img src={game.backgroundImageUrl} alt="" className="h-6 w-8 rounded object-cover" />
                    ) : (
                      <div className="flex h-6 w-8 items-center justify-center rounded bg-surface-active text-xs">🎮</div>
                    )}
                    <span className="truncate text-foreground">{game.name}</span>
                  </button>
                ))}
              </div>
            )}
          </div>
        </div>

        {activeTab === 'players' && (
          <PlayersSection players={players} isLoading={playersLoading} />
        )}

        {activeTab === 'rooms' && (
          <RoomsSection rooms={rooms} isLoading={roomsLoading} />
        )}
      </div>
    </AnimatedPage>
  );
}

function TabButton({ label, active, count, onClick }: {
  label: string;
  active: boolean;
  count?: number;
  onClick: () => void;
}) {
  return (
    <button
      onClick={onClick}
      className={`rounded-lg border px-4 py-2 text-sm font-medium transition-colors ${
        active
          ? 'border-primary/40 bg-primary/10 text-primary'
          : 'border-border text-foreground-muted hover:border-border-hover'
      }`}
    >
      {label}
      {count !== undefined && (
        <span className={`ml-1.5 text-xs ${active ? 'text-primary/70' : 'text-foreground-subtle'}`}>
          ({count})
        </span>
      )}
    </button>
  );
}

function PlayersSection({ players, isLoading }: {
  players: RecommendedPlayerResponse[] | undefined;
  isLoading: boolean;
}) {
  if (isLoading) {
    return (
      <div className="flex items-center justify-center py-20">
        <Spinner size="lg" />
      </div>
    );
  }

  if (!players || players.length === 0) {
    return (
      <EmptyState
        title="No player recommendations yet"
        description="Add game profiles to get personalized player suggestions based on your games, region, and experience."
        icon={<span className="text-3xl">🎮</span>}
        action={
          <Link
            to="/profile/games"
            className="rounded-lg bg-primary px-4 py-2 text-sm font-medium text-white hover:bg-primary-hover transition-colors"
          >
            Add Game Profiles
          </Link>
        }
      />
    );
  }

  return (
    <motion.div
      variants={staggerContainer}
      initial="hidden"
      animate="show"
      className="grid gap-4 sm:grid-cols-2 lg:grid-cols-3"
    >
      {players.map((player) => (
        <RecommendedPlayerCard key={player.id} player={player} />
      ))}
    </motion.div>
  );
}

function RoomsSection({ rooms, isLoading }: {
  rooms: RecommendedRoomResponse[] | undefined;
  isLoading: boolean;
}) {
  if (isLoading) {
    return (
      <div className="flex items-center justify-center py-20">
        <Spinner size="lg" />
      </div>
    );
  }

  if (!rooms || rooms.length === 0) {
    return (
      <EmptyState
        title="No room recommendations yet"
        description="Add game profiles to get personalized room suggestions based on your games, region, and language."
        icon={<span className="text-3xl">🚪</span>}
        action={
          <Link
            to="/profile/games"
            className="rounded-lg bg-primary px-4 py-2 text-sm font-medium text-white hover:bg-primary-hover transition-colors"
          >
            Add Game Profiles
          </Link>
        }
      />
    );
  }

  return (
    <motion.div
      variants={staggerContainer}
      initial="hidden"
      animate="show"
      className="grid gap-4 sm:grid-cols-2 lg:grid-cols-3"
    >
      {rooms.map((room) => (
        <RecommendedRoomCard key={room.id} room={room} />
      ))}
    </motion.div>
  );
}

function RecommendedPlayerCard({ player }: { player: RecommendedPlayerResponse }) {
  const meta = LEVEL_META[player.experienceLevel ?? ''];

  return (
    <motion.div variants={staggerItem}>
      <Link to={`/profile/${player.id}`}>
        <motion.div
          whileHover={{ y: -4, scale: 1.01 }}
          transition={{ duration: 0.2 }}
          className="group relative overflow-hidden rounded-xl border border-border bg-surface transition-colors hover:border-primary/30"
        >
          <div className="relative h-20 overflow-hidden bg-linear-to-br from-primary/15 via-accent/10 to-surface">
            <div className="absolute inset-0 bg-linear-to-b from-transparent to-surface" />
            <div className="absolute right-3 top-3 flex gap-1.5">
              <Badge variant="primary" className="border border-primary/30 shadow-sm">
                {Math.round(player.score)} pts
              </Badge>
              {player.lookingForTeam && (
                <Badge variant="accent" className="border border-accent/30 shadow-sm">🎯 LFT</Badge>
              )}
            </div>
          </div>

          <div className="relative -mt-8 px-5">
            <motion.div whileHover={{ scale: 1.1 }} className="inline-block rounded-full border-2 border-surface p-0.5">
              <UserAvatar username={player.username} avatarUrl={player.avatarUrl} size="lg" />
            </motion.div>
          </div>

          <div className="px-5 pb-5 pt-2">
            <h3 className="text-base font-bold text-foreground">{player.username}</h3>

            {player.bio && (
              <p className="mt-1 line-clamp-2 text-xs leading-relaxed text-foreground-muted">{player.bio}</p>
            )}

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

            {player.games.length > 0 && (
              <div className="mt-3 space-y-1.5">
                {player.games.slice(0, 2).map((game) => (
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
              </div>
            )}

            {player.matchReasons.length > 0 && (
              <div className="mt-3 border-t border-border/50 pt-2.5">
                <div className="flex flex-wrap gap-1">
                  {player.matchReasons.slice(0, 3).map((reason) => (
                    <span
                      key={reason}
                      className="rounded-full bg-success/10 px-2 py-0.5 text-[10px] font-medium text-success"
                    >
                      ✓ {reason}
                    </span>
                  ))}
                </div>
              </div>
            )}

            <div className="mt-3 flex justify-end">
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

function RecommendedRoomCard({ room }: { room: RecommendedRoomResponse }) {
  const capacityPercent = (room.currentMemberCount / room.maxMembers) * 100;
  const isFull = capacityPercent >= 100;

  return (
    <motion.div variants={staggerItem}>
      <Link to={`/rooms/${room.id}`}>
        <motion.div
          whileHover={{ y: -3 }}
          transition={{ duration: 0.2 }}
          className="group relative overflow-hidden rounded-xl border border-border bg-surface transition-colors hover:border-primary/30"
        >
          {room.gameImageUrl ? (
            <div className="relative h-28 overflow-hidden">
              <img
                src={room.gameImageUrl}
                alt={room.gameName ?? 'Game'}
                className="h-full w-full object-cover transition-transform duration-500 group-hover:scale-105"
              />
              <div className="absolute inset-0 bg-linear-to-t from-surface via-surface/50 to-transparent" />
              {room.gameName && (
                <span className="absolute bottom-2.5 left-3 rounded-md bg-black/50 px-2 py-0.5 text-xs font-medium text-white/90 backdrop-blur-sm">
                  {room.gameName}
                </span>
              )}
              <Badge
                variant="primary"
                className="absolute right-2.5 top-2.5 shadow-sm"
              >
                {Math.round(room.score)} pts
              </Badge>
            </div>
          ) : (
            <div className="relative flex h-20 items-center justify-center bg-linear-to-br from-primary/15 to-accent/10">
              <span className="text-xs text-foreground-muted">{room.gameName ?? 'Unknown Game'}</span>
              <Badge
                variant="primary"
                className="absolute right-2.5 top-2.5"
              >
                {Math.round(room.score)} pts
              </Badge>
            </div>
          )}

          <div className="p-4">
            <h3 className="font-semibold text-foreground line-clamp-1 group-hover:text-primary transition-colors">
              {room.title}
            </h3>

            <div className="mt-2.5 flex flex-wrap gap-1.5">
              <Badge>{room.region}</Badge>
              <Badge>{room.language}</Badge>
              {room.tags.slice(0, 2).map((tag) => (
                <Badge key={tag} variant="primary">{tag}</Badge>
              ))}
            </div>

            <div className="mt-3">
              <div className="flex items-center justify-between text-xs text-foreground-muted">
                <span className="flex items-center gap-1.5">
                  <svg className="h-3.5 w-3.5" fill="none" stroke="currentColor" viewBox="0 0 24 24" strokeWidth={1.5}>
                    <path strokeLinecap="round" strokeLinejoin="round" d="M15 19.128a9.38 9.38 0 002.625.372 9.337 9.337 0 004.121-.952 4.125 4.125 0 00-7.533-2.493M15 19.128v-.003c0-1.113-.285-2.16-.786-3.07M15 19.128v.106A12.318 12.318 0 018.624 21c-2.331 0-4.512-.645-6.374-1.766l-.001-.109a6.375 6.375 0 0111.964-3.07M12 6.375a3.375 3.375 0 11-6.75 0 3.375 3.375 0 016.75 0zm8.25 2.25a2.625 2.625 0 11-5.25 0 2.625 2.625 0 015.25 0z" />
                  </svg>
                  {room.currentMemberCount} / {room.maxMembers}
                </span>
                <span className={isFull ? 'font-medium text-danger' : ''}>{Math.round(capacityPercent)}%</span>
              </div>
              <div className="mt-1.5 h-2 w-full overflow-hidden rounded-full bg-surface-hover">
                <motion.div
                  initial={{ width: 0 }}
                  animate={{ width: `${capacityPercent}%` }}
                  transition={{ duration: 0.6, delay: 0.2 }}
                  className={`h-full rounded-full ${
                    isFull ? 'bg-danger' : capacityPercent >= 70 ? 'bg-warning' : 'bg-accent'
                  }`}
                />
              </div>
            </div>

            {room.matchReasons.length > 0 && (
              <div className="mt-3 border-t border-border/50 pt-2.5">
                <div className="flex flex-wrap gap-1">
                  {room.matchReasons.slice(0, 3).map((reason) => (
                    <span
                      key={reason}
                      className="rounded-full bg-success/10 px-2 py-0.5 text-[10px] font-medium text-success"
                    >
                      ✓ {reason}
                    </span>
                  ))}
                </div>
              </div>
            )}

            <div className="mt-3 flex items-center justify-between">
              <span className="text-xs text-foreground-subtle">
                {new Date(room.createdAt).toLocaleDateString(undefined, { month: 'short', day: 'numeric' })}
              </span>
              <span className="text-xs font-medium text-primary transition-colors group-hover:text-primary-hover">
                {room.status === 'Open' ? 'Join →' : room.status}
              </span>
            </div>
          </div>
        </motion.div>
      </Link>
    </motion.div>
  );
}
