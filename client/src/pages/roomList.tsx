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

  const { data, isLoading } = useRooms(filters);

  return (
    <AnimatedPage>
      <div className="space-y-6">
        {/* Header */}
        <div className="flex flex-col gap-4 sm:flex-row sm:items-center sm:justify-between">
          <motion.div initial={{ opacity: 0, x: -20 }} animate={{ opacity: 1, x: 0 }}>
            <h1 className="text-3xl font-bold text-foreground">Rooms</h1>
            <p className="mt-1 text-foreground-muted">Browse and join rooms to find teammates</p>
          </motion.div>
          <motion.div initial={{ opacity: 0, x: 20 }} animate={{ opacity: 1, x: 0 }}>
            <motion.div whileHover={{ scale: 1.05 }} whileTap={{ scale: 0.98 }}>
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
          </motion.div>
        </div>

        {/* Filters */}
        <motion.div
          initial={{ opacity: 0, y: 10 }}
          animate={{ opacity: 1, y: 0 }}
          transition={{ delay: 0.1 }}
          className="flex flex-wrap gap-3"
        >
          <div className="w-full sm:w-48">
            <Select
              id="region"
              options={regionOptions}
              value={filters.region ?? ''}
              onChange={(e) => setFilters({ ...filters, region: e.target.value as RoomFilters['region'], page: 1 })}
              placeholder="All Regions"
            />
          </div>
          <div className="w-full sm:w-48">
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

  return (
    <motion.div variants={staggerItem}>
      <Link to={`/rooms/${room.id}`}>
        <motion.div
          whileHover={{ y: -4, scale: 1.01 }}
          className="group relative overflow-hidden rounded-xl border border-border bg-surface transition-colors hover:border-primary/30"
        >
          {room.gameImageUrl ? (
            <div className="relative h-32 overflow-hidden">
              <img
                src={room.gameImageUrl}
                alt={room.gameName ?? 'Game'}
                className="h-full w-full object-cover transition-transform duration-300 group-hover:scale-105"
              />
              <div className="absolute inset-0 bg-gradient-to-t from-surface via-surface/60 to-transparent" />
              {room.gameName && (
                <span className="absolute bottom-2 left-3 text-xs font-medium text-white/80 bg-black/40 px-2 py-0.5 rounded">
                  {room.gameName}
                </span>
              )}
            </div>
          ) : (
            <div className="h-16 bg-gradient-to-br from-primary/20 to-accent/10 flex items-center justify-center">
              <span className="text-xs text-foreground-muted">{room.gameName ?? 'Unknown Game'}</span>
            </div>
          )}

          <div className="p-4">
            <div className="flex items-start justify-between">
              <h3 className="font-semibold text-foreground line-clamp-1">{room.title}</h3>
              <Badge variant={statusColors[room.status] ?? 'default'}>{room.status}</Badge>
            </div>

            <div className="mt-2.5 flex flex-wrap gap-1.5">
              <Badge>{room.region}</Badge>
              <Badge>{room.language}</Badge>
              {room.tags.slice(0, 2).map((tag) => (
                <Badge key={tag} variant="primary">{tag}</Badge>
              ))}
            </div>

            <div className="mt-3">
              <div className="flex items-center justify-between text-xs text-foreground-muted">
                <span>{room.currentMemberCount} / {room.maxMembers} members</span>
                <span>{Math.round(capacityPercent)}%</span>
              </div>
              <div className="mt-1.5 h-1.5 w-full overflow-hidden rounded-full bg-surface-hover">
                <motion.div
                  initial={{ width: 0 }}
                  animate={{ width: `${capacityPercent}%` }}
                  transition={{ duration: 0.6, delay: 0.2 }}
                  className={`h-full rounded-full ${
                    capacityPercent >= 100
                      ? 'bg-danger'
                      : capacityPercent >= 70
                        ? 'bg-warning'
                        : 'bg-accent'
                  }`}
                />
              </div>
            </div>

            <div className="mt-3 flex items-center justify-between">
              <span className="text-xs text-foreground-subtle">
                {new Date(room.createdAt).toLocaleDateString()}
              </span>
              <Button variant="ghost" size="sm" className="opacity-0 transition-opacity group-hover:opacity-100">
                Join →
              </Button>
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
                        className="h-8 w-12 rounded object-cover flex-shrink-0"
                      />
                    ) : (
                      <div className="h-8 w-12 rounded bg-surface-hover flex-shrink-0" />
                    )}
                    <div className="min-w-0 flex-1">
                      <p className="truncate font-medium">{g.name}</p>
                      {g.genres.length > 0 && (
                        <p className="truncate text-xs text-foreground-subtle">{g.genres.slice(0, 3).join(', ')}</p>
                      )}
                    </div>
                    {g.metacritic && (
                      <span className={`flex-shrink-0 rounded px-1.5 py-0.5 text-xs font-bold ${
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
            Failed to create room. Please try again.
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
