import { useState } from 'react';
import { Link, useNavigate } from 'react-router-dom';
import { useForm, Controller } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { motion, AnimatePresence } from 'framer-motion';
import { useRooms, useCreateRoom } from '@/features/rooms/hooks';
import { createRoomSchema, type CreateRoomFormData } from '@/features/rooms/schemas';
import { useGameSearch } from '@/features/games/hooks';
import { useDebounce } from '@/hooks/useDebounce';
import { useAuthStore } from '@/stores/authStore';
import type { RoomFilters } from '@/types/api';
import type { RoomResponse } from '@/features/rooms/types';
import {
  Button, Input, Select, Textarea, Modal, Badge,
  AnimatedPage, Spinner, staggerContainer, staggerItem,
} from '@/components/ui';

const regionOptions = [
  { value: '', label: 'All Regions' },
  { value: 'EU', label: 'Europe' },
  { value: 'NA', label: 'North America' },
  { value: 'TR', label: 'Turkey' },
  { value: 'AS', label: 'Asia' },
  { value: 'SA', label: 'South America' },
  { value: 'OCE', label: 'Oceania' },
];

const languageOptions = [
  { value: '', label: 'All Languages' },
  { value: 'English', label: 'English' },
  { value: 'Turkish', label: 'Turkish' },
  { value: 'German', label: 'German' },
  { value: 'French', label: 'French' },
  { value: 'Spanish', label: 'Spanish' },
  { value: 'Russian', label: 'Russian' },
];

const statusColors: Record<string, 'success' | 'warning' | 'primary' | 'danger' | 'default'> = {
  Open: 'success',
  Full: 'warning',
  InProgress: 'primary',
  Closed: 'danger',
};

export default function RoomListPage() {
  const navigate = useNavigate();
  const isAuth = useAuthStore((s) => s.isAuthenticated());
  const [showCreateModal, setShowCreateModal] = useState(false);
  const [filters, setFilters] = useState<RoomFilters>({ page: 1, pageSize: 12 });

  const [gameFilterSearch, setGameFilterSearch] = useState('');
  const [gameFilterName, setGameFilterName] = useState('');
  const [showGameDropdown, setShowGameDropdown] = useState(false);
  const debouncedGameFilter = useDebounce(gameFilterSearch, 300);
  const { data: filterGames } = useGameSearch(debouncedGameFilter);

  const { data, isLoading } = useRooms(filters);

  const clearGameFilter = () => {
    setFilters((f) => ({ ...f, gameId: undefined, page: 1 }));
    setGameFilterSearch('');
    setGameFilterName('');
  };

  return (
    <AnimatedPage>
      <div className="space-y-6">
        <div className="flex flex-col gap-4 sm:flex-row sm:items-center sm:justify-between">
          <motion.div initial={{ opacity: 0, x: -20 }} animate={{ opacity: 1, x: 0 }}>
            <div className="flex items-baseline gap-3">
              <h1 className="text-3xl font-bold text-foreground">Rooms</h1>
              {data && (
                <span className="text-sm text-foreground-subtle">{data.totalCount} total</span>
              )}
            </div>
            <p className="mt-1 text-sm text-foreground-muted">Browse and join rooms to find teammates</p>
          </motion.div>
          <motion.div initial={{ opacity: 0, x: 20 }} animate={{ opacity: 1, x: 0 }}>
            <Button
              onClick={() => isAuth ? setShowCreateModal(true) : navigate('/login')}
              className="gap-2"
            >
              <svg className="h-4 w-4" fill="none" stroke="currentColor" viewBox="0 0 24 24" strokeWidth={2}>
                <path strokeLinecap="round" strokeLinejoin="round" d="M12 4.5v15m7.5-7.5h-15" />
              </svg>
              Create Room
            </Button>
          </motion.div>
        </div>

        <motion.div
          initial={{ opacity: 0, y: 10 }}
          animate={{ opacity: 1, y: 0 }}
          transition={{ delay: 0.1 }}
          className="flex flex-wrap items-end gap-3 rounded-xl border border-border/60 bg-surface/80 p-4"
        >
          <div className="relative w-full sm:w-56">
            {filters.gameId ? (
              <div className="flex h-[38px] items-center gap-2 rounded-md border border-primary/40 bg-primary/5 px-3">
                <span className="text-xs">🎮</span>
                <span className="flex-1 truncate text-sm font-medium text-foreground">{gameFilterName}</span>
                <button onClick={clearGameFilter} className="text-foreground-muted hover:text-danger transition-colors">
                  <svg className="h-3.5 w-3.5" fill="none" stroke="currentColor" viewBox="0 0 24 24" strokeWidth={2.5}>
                    <path strokeLinecap="round" strokeLinejoin="round" d="M6 18L18 6M6 6l12 12" />
                  </svg>
                </button>
              </div>
            ) : (
              <>
                <Input
                  id="gameFilter"
                  placeholder="Filter by game..."
                  value={gameFilterSearch}
                  onChange={(e) => { setGameFilterSearch(e.target.value); setShowGameDropdown(true); }}
                  onFocus={() => setShowGameDropdown(true)}
                />
                <AnimatePresence>
                  {showGameDropdown && filterGames && filterGames.length > 0 && (
                    <>
                      <div className="fixed inset-0 z-30" onClick={() => setShowGameDropdown(false)} />
                      <motion.div
                        initial={{ opacity: 0, y: -4 }}
                        animate={{ opacity: 1, y: 0 }}
                        exit={{ opacity: 0, y: -4 }}
                        className="absolute left-0 top-full z-40 mt-1 max-h-52 w-72 overflow-y-auto rounded-lg border border-border bg-surface shadow-xl"
                      >
                        {filterGames.map((g) => (
                          <button
                            key={g.id}
                            className="flex w-full items-center gap-3 px-3 py-2 text-left text-sm text-foreground hover:bg-surface-hover transition-colors"
                            onClick={() => {
                              setFilters((f) => ({ ...f, gameId: g.id, page: 1 }));
                              setGameFilterName(g.name);
                              setGameFilterSearch('');
                              setShowGameDropdown(false);
                            }}
                          >
                            {g.backgroundImageUrl ? (
                              <img src={g.backgroundImageUrl} alt={g.name} className="h-7 w-10 shrink-0 rounded object-cover" />
                            ) : (
                              <div className="flex h-7 w-10 shrink-0 items-center justify-center rounded bg-surface-hover text-xs">🎮</div>
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

          <div className="w-full sm:w-44">
            <Select
              id="region"
              options={regionOptions}
              value={filters.region ?? ''}
              onChange={(e) => setFilters({ ...filters, region: e.target.value as RoomFilters['region'], page: 1 })}
              placeholder="All Regions"
            />
          </div>
          <div className="w-full sm:w-44">
            <Select
              id="language"
              options={languageOptions}
              value={filters.language ?? ''}
              onChange={(e) => setFilters({ ...filters, language: e.target.value as RoomFilters['language'], page: 1 })}
              placeholder="All Languages"
            />
          </div>
          <div className="flex gap-1">
            {(['Open', 'Full', 'InProgress'] as const).map((status) => (
              <button
                key={status}
                onClick={() => setFilters({ ...filters, status: filters.status === status ? undefined : status, page: 1 })}
                className={`rounded-lg border px-3 py-2 text-xs font-medium transition-colors ${
                  filters.status === status
                    ? 'border-primary bg-primary/10 text-primary'
                    : 'border-border text-foreground-muted hover:border-border-hover'
                }`}
              >
                {status}
              </button>
            ))}
          </div>
        </motion.div>

        {/* Room Grid */}
        {isLoading ? (
          <div className="flex justify-center py-20">
            <Spinner size="lg" />
          </div>
        ) : data && data.items.length > 0 ? (
          <>
            <motion.div
              variants={staggerContainer}
              initial="hidden"
              animate="show"
              className="grid gap-4 sm:grid-cols-2 lg:grid-cols-3"
            >
              {data.items.map((room) => (
                <RoomCard key={room.id} room={room} />
              ))}
            </motion.div>

            {/* Pagination */}
            {(data.hasNextPage || data.hasPreviousPage) && (
              <motion.div
                initial={{ opacity: 0 }}
                animate={{ opacity: 1 }}
                className="flex items-center justify-center gap-3"
              >
                <Button
                  variant="outline"
                  size="sm"
                  disabled={!data.hasPreviousPage}
                  onClick={() => setFilters({ ...filters, page: (filters.page ?? 1) - 1 })}
                >
                  Previous
                </Button>
                <span className="text-sm text-foreground-muted">
                  Page {data.page} of {Math.ceil(data.totalCount / data.pageSize)}
                </span>
                <Button
                  variant="outline"
                  size="sm"
                  disabled={!data.hasNextPage}
                  onClick={() => setFilters({ ...filters, page: (filters.page ?? 1) + 1 })}
                >
                  Next
                </Button>
              </motion.div>
            )}
          </>
        ) : (
          <motion.div
            initial={{ opacity: 0, scale: 0.95 }}
            animate={{ opacity: 1, scale: 1 }}
            className="flex flex-col items-center py-20 text-center"
          >
            <motion.div
              animate={{ y: [0, -8, 0] }}
              transition={{ duration: 2, repeat: Infinity }}
              className="text-5xl"
            >
              🏠
            </motion.div>
            <h3 className="mt-4 text-xl font-bold text-foreground">No rooms found</h3>
            <p className="mt-2 text-foreground-muted">Be the first to create a room!</p>
            <Button
              className="mt-4"
              onClick={() => isAuth ? setShowCreateModal(true) : navigate('/login')}
            >
              Create Room
            </Button>
          </motion.div>
        )}

        <CreateRoomModal
          isOpen={showCreateModal}
          onClose={() => setShowCreateModal(false)}
        />
      </div>
    </AnimatedPage>
  );
}

function RoomCard({ room }: { room: RoomResponse }) {
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
                variant={statusColors[room.status] ?? 'default'}
                className="absolute right-2.5 top-2.5 shadow-sm"
              >
                {room.status}
              </Badge>
            </div>
          ) : (
            <div className="relative flex h-20 items-center justify-center bg-linear-to-br from-primary/15 to-accent/10">
              <span className="text-xs text-foreground-muted">{room.gameName ?? 'Unknown Game'}</span>
              <Badge
                variant={statusColors[room.status] ?? 'default'}
                className="absolute right-2.5 top-2.5"
              >
                {room.status}
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

            <div className="mt-3.5 flex items-center justify-between">
              <span className="text-xs text-foreground-subtle">
                {new Date(room.createdAt).toLocaleDateString(undefined, { month: 'short', day: 'numeric' })}
              </span>
              <span className={`text-xs font-medium transition-colors ${
                room.status === 'Open'
                  ? 'text-primary group-hover:text-primary-hover'
                  : 'text-foreground-subtle'
              }`}>
                {room.status === 'Open' ? 'Join →' : room.status === 'Full' ? 'Full' : room.status}
              </span>
            </div>
          </div>
        </motion.div>
      </Link>
    </motion.div>
  );
}

function CreateRoomModal({ isOpen, onClose }: { isOpen: boolean; onClose: () => void }) {
  const navigate = useNavigate();
  const createRoom = useCreateRoom();
  const [gameSearch, setGameSearch] = useState('');
  const debouncedGameSearch = useDebounce(gameSearch, 300);
  const { data: games } = useGameSearch(debouncedGameSearch);

  const {
    register,
    handleSubmit,
    control,
    setValue,
    watch,
    formState: { errors },
    reset,
  } = useForm<CreateRoomFormData>({
    resolver: zodResolver(createRoomSchema),
    defaultValues: { maxMembers: 5, isPublic: true },
  });

  const selectedGameId = watch('gameId');

  const onSubmit = (data: CreateRoomFormData) => {
    createRoom.mutate(data, {
      onSuccess: (room) => {
        reset();
        onClose();
        navigate(`/rooms/${room.id}`);
      },
    });
  };

  return (
    <Modal isOpen={isOpen} onClose={onClose} title="Create Room" className="max-w-lg">
      <form onSubmit={handleSubmit(onSubmit)} className="space-y-4">
        <Input
          id="title"
          label="Room Title"
          placeholder="e.g. LFG Ranked Valorant"
          error={errors.title?.message}
          {...register('title')}
        />

        <div>
          <label className="mb-1.5 block text-sm font-medium text-foreground-muted">Game</label>
          <Input
            id="gameSearch"
            placeholder="Search for a game..."
            value={gameSearch}
            onChange={(e) => setGameSearch(e.target.value)}
          />
          {selectedGameId && (
            <div className="mt-2 flex items-center gap-3 rounded-lg border border-primary/30 bg-primary/5 p-2">
              {games?.find((g) => g.id === selectedGameId)?.backgroundImageUrl ? (
                <img
                  src={games.find((g) => g.id === selectedGameId)!.backgroundImageUrl!}
                  alt={gameSearch}
                  className="h-10 w-16 rounded object-cover"
                />
              ) : (
                <div className="h-10 w-16 rounded bg-surface-hover" />
              )}
              <span className="flex-1 text-sm font-medium text-foreground">{gameSearch}</span>
              <button
                type="button"
                className="text-foreground-muted hover:text-danger transition-colors"
                onClick={() => {
                  setValue('gameId', '' as string);
                  setGameSearch('');
                }}
              >
                <svg className="h-4 w-4" fill="none" stroke="currentColor" viewBox="0 0 24 24" strokeWidth={2}>
                  <path strokeLinecap="round" strokeLinejoin="round" d="M6 18L18 6M6 6l12 12" />
                </svg>
              </button>
            </div>
          )}
          <AnimatePresence>
            {games && games.length > 0 && !selectedGameId && (
              <motion.div
                initial={{ opacity: 0, height: 0 }}
                animate={{ opacity: 1, height: 'auto' }}
                exit={{ opacity: 0, height: 0 }}
                className="mt-1 max-h-48 overflow-y-auto rounded-md border border-border bg-surface"
              >
                {games.map((g) => (
                  <button
                    key={g.id}
                    type="button"
                    className="flex w-full items-center gap-3 px-3 py-2 text-left text-sm text-foreground hover:bg-surface-hover transition-colors"
                    onClick={() => {
                      setValue('gameId', g.id);
                      setGameSearch(g.name);
                    }}
                  >
                    {g.backgroundImageUrl ? (
                      <img
                        src={g.backgroundImageUrl}
                        alt={g.name}
                        className="h-8 w-12 rounded object-cover shrink-0"
                      />
                    ) : (
                      <div className="h-8 w-12 rounded bg-surface-hover shrink-0" />
                    )}
                    <div className="min-w-0 flex-1">
                      <p className="truncate font-medium">{g.name}</p>
                      {g.genres.length > 0 && (
                        <p className="truncate text-xs text-foreground-subtle">{g.genres.slice(0, 3).join(', ')}</p>
                      )}
                    </div>
                    {g.metacritic && (
                      <span className={`shrink-0 rounded px-1.5 py-0.5 text-xs font-bold ${
                        g.metacritic >= 75 ? 'bg-success/20 text-success' :
                        g.metacritic >= 50 ? 'bg-warning/20 text-warning' :
                        'bg-danger/20 text-danger'
                      }`}>
                        {g.metacritic}
                      </span>
                    )}
                  </button>
                ))}
              </motion.div>
            )}
          </AnimatePresence>
          {errors.gameId && <p className="mt-1 text-xs text-danger">{errors.gameId.message}</p>}
        </div>

        <Textarea
          id="description"
          label="Description (optional)"
          placeholder="Describe what you're looking for..."
          rows={3}
          {...register('description')}
        />

        <div className="grid grid-cols-2 gap-3">
          <Controller
            name="region"
            control={control}
            render={({ field }) => (
              <Select
                id="roomRegion"
                label="Region"
                options={regionOptions.filter((o) => o.value)}
                placeholder="Select"
                error={errors.region?.message}
                {...field}
              />
            )}
          />
          <Controller
            name="language"
            control={control}
            render={({ field }) => (
              <Select
                id="roomLanguage"
                label="Language"
                options={languageOptions.filter((o) => o.value)}
                placeholder="Select"
                error={errors.language?.message}
                {...field}
              />
            )}
          />
        </div>

        <Input
          id="maxMembers"
          type="number"
          label="Max Members"
          min={2}
          max={10}
          error={errors.maxMembers?.message}
          {...register('maxMembers', { valueAsNumber: true })}
        />

        {createRoom.error && (
          <motion.p
            initial={{ opacity: 0 }}
            animate={{ opacity: 1 }}
            className="rounded-md bg-danger/10 px-3 py-2 text-sm text-danger"
          >
            {(createRoom.error as any)?.response?.data?.title ?? 'Failed to create room. Please try again.'}
          </motion.p>
        )}

        <div className="flex justify-end gap-3 pt-2">
          <Button variant="ghost" type="button" onClick={onClose}>Cancel</Button>
          <Button type="submit" isLoading={createRoom.isPending}>Create Room</Button>
        </div>
      </form>
    </Modal>
  );
}
