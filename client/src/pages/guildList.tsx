import { useState } from 'react';
import { Link, useNavigate } from 'react-router-dom';
import { useForm, Controller } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { motion, AnimatePresence } from 'framer-motion';
import { useGuilds, useCreateGuild } from '@/features/guilds/hooks';
import { createGuildSchema, type CreateGuildFormData } from '@/features/guilds/schemas';
import { useGameSearch } from '@/features/games/hooks';
import { useDebounce } from '@/hooks/useDebounce';
import { useAuthStore } from '@/stores/authStore';
import type { GuildFilters } from '@/types/api';
import type { GuildResponse } from '@/features/guilds/types';
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

export default function GuildListPage() {
  const navigate = useNavigate();
  const isAuth = useAuthStore((s) => s.isAuthenticated());
  const [showCreateModal, setShowCreateModal] = useState(false);
  const [filters, setFilters] = useState<GuildFilters>({ page: 1, pageSize: 12 });

  const [gameFilterSearch, setGameFilterSearch] = useState('');
  const [gameFilterName, setGameFilterName] = useState('');
  const [showGameDropdown, setShowGameDropdown] = useState(false);
  const debouncedGameFilter = useDebounce(gameFilterSearch, 300);
  const { data: filterGames } = useGameSearch(debouncedGameFilter);

  const [searchInput, setSearchInput] = useState('');
  const debouncedSearch = useDebounce(searchInput, 400);

  const activeFilters: GuildFilters = {
    ...filters,
    search: debouncedSearch || undefined,
  };

  const { data, isLoading } = useGuilds(activeFilters);

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
              <h1 className="text-3xl font-bold text-foreground">Guilds</h1>
              {data && (
                <span className="text-sm text-foreground-subtle">{data.totalCount} total</span>
              )}
            </div>
            <p className="mt-1 text-sm text-foreground-muted">
              Find a permanent community to play with
            </p>
          </motion.div>
          <motion.div initial={{ opacity: 0, x: 20 }} animate={{ opacity: 1, x: 0 }}>
            <Button
              onClick={() => (isAuth ? setShowCreateModal(true) : navigate('/login'))}
              className="gap-2"
            >
              <svg className="h-4 w-4" fill="none" stroke="currentColor" viewBox="0 0 24 24" strokeWidth={2}>
                <path strokeLinecap="round" strokeLinejoin="round" d="M12 4.5v15m7.5-7.5h-15" />
              </svg>
              Create Guild
            </Button>
          </motion.div>
        </div>

        {/* Filters */}
        <motion.div
          initial={{ opacity: 0, y: 10 }}
          animate={{ opacity: 1, y: 0 }}
          transition={{ delay: 0.1 }}
          className="flex flex-wrap items-end gap-3 rounded-xl border border-border/60 bg-surface/80 p-4"
        >
          <div className="w-full sm:w-56">
            <Input
              id="guildSearch"
              placeholder="Search guilds..."
              value={searchInput}
              onChange={(e) => {
                setSearchInput(e.target.value);
                setFilters((f) => ({ ...f, page: 1 }));
              }}
            />
          </div>

          <div className="relative w-full sm:w-56">
            {filters.gameId ? (
              <div className="flex h-[38px] items-center gap-2 rounded-md border border-primary/40 bg-primary/5 px-3">
                <span className="text-xs">🎮</span>
                <span className="flex-1 truncate text-sm font-medium text-foreground">
                  {gameFilterName}
                </span>
                <button
                  onClick={clearGameFilter}
                  className="text-foreground-muted hover:text-danger transition-colors"
                >
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
                  onChange={(e) => {
                    setGameFilterSearch(e.target.value);
                    setShowGameDropdown(true);
                  }}
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
              onChange={(e) =>
                setFilters({ ...filters, region: e.target.value as GuildFilters['region'], page: 1 })
              }
              placeholder="All Regions"
            />
          </div>
          <div className="w-full sm:w-44">
            <Select
              id="language"
              options={languageOptions}
              value={filters.language ?? ''}
              onChange={(e) =>
                setFilters({ ...filters, language: e.target.value as GuildFilters['language'], page: 1 })
              }
              placeholder="All Languages"
            />
          </div>
        </motion.div>

        {/* Guild Grid */}
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
              {data.items.map((guild) => (
                <GuildCard key={guild.id} guild={guild} />
              ))}
            </motion.div>

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
              ⚔️
            </motion.div>
            <h3 className="mt-4 text-xl font-bold text-foreground">No guilds found</h3>
            <p className="mt-2 text-foreground-muted">Be the first to create a guild!</p>
            <Button
              className="mt-4"
              onClick={() => (isAuth ? setShowCreateModal(true) : navigate('/login'))}
            >
              Create Guild
            </Button>
          </motion.div>
        )}

        <CreateGuildModal isOpen={showCreateModal} onClose={() => setShowCreateModal(false)} />
      </div>
    </AnimatedPage>
  );
}

function GuildCard({ guild }: { guild: GuildResponse }) {
  const capacityPercent = guild.maxMembers > 0
    ? (guild.currentMemberCount / guild.maxMembers) * 100
    : 0;

  return (
    <motion.div variants={staggerItem}>
      <Link to={`/guilds/${guild.id}`}>
        <motion.div
          whileHover={{ y: -3 }}
          transition={{ duration: 0.2 }}
          className="group relative overflow-hidden rounded-xl border border-border bg-surface transition-colors hover:border-primary/30"
        >
          <div className="relative flex h-24 items-center justify-between bg-linear-to-br from-primary/15 via-accent/10 to-primary/5 px-5">
            <div className="min-w-0">
              <div className="flex items-center gap-2.5">
                <span className="shrink-0 rounded-md bg-primary/20 px-2 py-0.5 text-xs font-bold text-primary">
                  [{guild.tag}]
                </span>
                <h3 className="truncate text-lg font-bold text-foreground group-hover:text-primary transition-colors">
                  {guild.name}
                </h3>
              </div>
              {guild.gameNames.length > 0 && (
                <p className="mt-1 truncate text-xs text-foreground-muted">
                  {guild.gameNames.slice(0, 3).join(', ')}
                  {guild.gameNames.length > 3 && ` +${guild.gameNames.length - 3}`}
                </p>
              )}
            </div>
            <div className="flex shrink-0 flex-col items-end gap-1">
              <span className="text-2xl font-bold text-primary/60">{guild.currentMemberCount}</span>
              <span className="text-[10px] text-foreground-subtle">members</span>
            </div>
          </div>

          <div className="p-4">
            {guild.description && (
              <p className="mb-3 text-sm text-foreground-muted line-clamp-2">{guild.description}</p>
            )}

            <div className="flex flex-wrap gap-1.5">
              <Badge>{guild.region}</Badge>
              <Badge>{guild.language}</Badge>
            </div>

            <div className="mt-3">
              <div className="flex items-center justify-between text-xs text-foreground-muted">
                <span className="flex items-center gap-1.5">
                  <svg className="h-3.5 w-3.5" fill="none" stroke="currentColor" viewBox="0 0 24 24" strokeWidth={1.5}>
                    <path strokeLinecap="round" strokeLinejoin="round" d="M15 19.128a9.38 9.38 0 002.625.372 9.337 9.337 0 004.121-.952 4.125 4.125 0 00-7.533-2.493M15 19.128v-.003c0-1.113-.285-2.16-.786-3.07M15 19.128v.106A12.318 12.318 0 018.624 21c-2.331 0-4.512-.645-6.374-1.766l-.001-.109a6.375 6.375 0 0111.964-3.07M12 6.375a3.375 3.375 0 11-6.75 0 3.375 3.375 0 016.75 0zm8.25 2.25a2.625 2.625 0 11-5.25 0 2.625 2.625 0 015.25 0z" />
                  </svg>
                  {guild.currentMemberCount} / {guild.maxMembers}
                </span>
                <span>{Math.round(capacityPercent)}%</span>
              </div>
              <div className="mt-1.5 h-2 w-full overflow-hidden rounded-full bg-surface-hover">
                <motion.div
                  initial={{ width: 0 }}
                  animate={{ width: `${Math.min(capacityPercent, 100)}%` }}
                  transition={{ duration: 0.6, delay: 0.2 }}
                  className={`h-full rounded-full ${
                    capacityPercent >= 90 ? 'bg-danger' : capacityPercent >= 70 ? 'bg-warning' : 'bg-accent'
                  }`}
                />
              </div>
            </div>

            <div className="mt-3.5 flex items-center justify-between">
              <span className="text-xs text-foreground-subtle">
                {new Date(guild.createdAt).toLocaleDateString(undefined, { month: 'short', day: 'numeric' })}
              </span>
              <span className="text-xs font-medium text-primary group-hover:text-primary-hover transition-colors">
                View Guild →
              </span>
            </div>
          </div>
        </motion.div>
      </Link>
    </motion.div>
  );
}

function CreateGuildModal({ isOpen, onClose }: { isOpen: boolean; onClose: () => void }) {
  const navigate = useNavigate();
  const createGuild = useCreateGuild();
  const [gameSearch, setGameSearch] = useState('');
  const debouncedGameSearch = useDebounce(gameSearch, 300);
  const { data: games } = useGameSearch(debouncedGameSearch);
  const [selectedGames, setSelectedGames] = useState<{ id: string; name: string }[]>([]);

  const {
    register,
    handleSubmit,
    control,
    formState: { errors },
    reset,
    setValue,
  } = useForm<CreateGuildFormData>({
    resolver: zodResolver(createGuildSchema),
    defaultValues: { isPublic: true, gameIds: [] },
  });

  const addGame = (id: string, name: string) => {
    if (selectedGames.length >= 10 || selectedGames.some((g) => g.id === id)) return;
    const next = [...selectedGames, { id, name }];
    setSelectedGames(next);
    setValue('gameIds', next.map((g) => g.id));
    setGameSearch('');
  };

  const removeGame = (id: string) => {
    const next = selectedGames.filter((g) => g.id !== id);
    setSelectedGames(next);
    setValue('gameIds', next.map((g) => g.id));
  };

  const onSubmit = (data: CreateGuildFormData) => {
    createGuild.mutate(data, {
      onSuccess: (guild) => {
        reset();
        setSelectedGames([]);
        onClose();
        navigate(`/guilds/${guild.id}`);
      },
    });
  };

  return (
    <Modal isOpen={isOpen} onClose={onClose} title="Create Guild" className="max-w-lg">
      <form onSubmit={handleSubmit(onSubmit)} className="space-y-4">
        <div className="grid grid-cols-2 gap-3">
          <Input
            id="name"
            label="Guild Name"
            placeholder="e.g. Shadow Legion"
            error={errors.name?.message}
            {...register('name')}
          />
          <Input
            id="tag"
            label="Tag"
            placeholder="e.g. SL"
            error={errors.tag?.message}
            {...register('tag')}
          />
        </div>

        <Textarea
          id="description"
          label="Description (optional)"
          placeholder="Tell people about your guild..."
          rows={3}
          {...register('description')}
        />

        <div>
          <label className="mb-1.5 block text-sm font-medium text-foreground-muted">Games</label>
          {selectedGames.length > 0 && (
            <div className="mb-2 flex flex-wrap gap-1.5">
              {selectedGames.map((g) => (
                <span
                  key={g.id}
                  className="flex items-center gap-1.5 rounded-md border border-primary/30 bg-primary/5 px-2 py-1 text-xs font-medium text-foreground"
                >
                  {g.name}
                  <button type="button" onClick={() => removeGame(g.id)} className="text-foreground-muted hover:text-danger transition-colors">
                    <svg className="h-3 w-3" fill="none" stroke="currentColor" viewBox="0 0 24 24" strokeWidth={2.5}>
                      <path strokeLinecap="round" strokeLinejoin="round" d="M6 18L18 6M6 6l12 12" />
                    </svg>
                  </button>
                </span>
              ))}
            </div>
          )}
          <Input
            id="gameSearch"
            placeholder="Search for games to add..."
            value={gameSearch}
            onChange={(e) => setGameSearch(e.target.value)}
          />
          <AnimatePresence>
            {games && games.length > 0 && gameSearch && (
              <motion.div
                initial={{ opacity: 0, height: 0 }}
                animate={{ opacity: 1, height: 'auto' }}
                exit={{ opacity: 0, height: 0 }}
                className="mt-1 max-h-48 overflow-y-auto rounded-md border border-border bg-surface"
              >
                {games
                  .filter((g) => !selectedGames.some((s) => s.id === g.id))
                  .map((g) => (
                    <button
                      key={g.id}
                      type="button"
                      className="flex w-full items-center gap-3 px-3 py-2 text-left text-sm text-foreground hover:bg-surface-hover transition-colors"
                      onClick={() => addGame(g.id, g.name)}
                    >
                      {g.backgroundImageUrl ? (
                        <img src={g.backgroundImageUrl} alt={g.name} className="h-8 w-12 rounded object-cover shrink-0" />
                      ) : (
                        <div className="h-8 w-12 rounded bg-surface-hover shrink-0" />
                      )}
                      <span className="min-w-0 flex-1 truncate font-medium">{g.name}</span>
                    </button>
                  ))}
              </motion.div>
            )}
          </AnimatePresence>
          {errors.gameIds && <p className="mt-1 text-xs text-danger">{errors.gameIds.message}</p>}
        </div>

        <div className="grid grid-cols-2 gap-3">
          <Controller
            name="region"
            control={control}
            render={({ field }) => (
              <Select
                id="guildRegion"
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
                id="guildLanguage"
                label="Language"
                options={languageOptions.filter((o) => o.value)}
                placeholder="Select"
                error={errors.language?.message}
                {...field}
              />
            )}
          />
        </div>

        {createGuild.error && (
          <motion.p
            initial={{ opacity: 0 }}
            animate={{ opacity: 1 }}
            className="rounded-md bg-danger/10 px-3 py-2 text-sm text-danger"
          >
            {(createGuild.error as any)?.response?.data?.title ?? 'Failed to create guild. Please try again.'}
          </motion.p>
        )}

        <div className="flex justify-end gap-3 pt-2">
          <Button variant="ghost" type="button" onClick={onClose}>Cancel</Button>
          <Button type="submit" isLoading={createGuild.isPending}>Create Guild</Button>
        </div>
      </form>
    </Modal>
  );
}
