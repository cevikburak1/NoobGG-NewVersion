import { useState, useCallback } from 'react';
import { motion, AnimatePresence } from 'framer-motion';
import {
  useMyProfile,
  useAddGameProfile,
  useUpdateGameProfile,
  useDeleteGameProfile,
} from '@/features/profile/hooks';
import { useGameSearch } from '@/features/games/hooks';
import {
  Button,
  Input,
  Select,
  Card,
  Badge,
  AnimatedPage,
  Spinner,
  Modal,
  staggerContainer,
  staggerItem,
} from '@/components/ui';
import type { GameResponse } from '@/features/games/types';
import type { GameProfileResponse } from '@/features/profile/types';

const REGIONS = ['EU', 'NA', 'SA', 'AS', 'OCE', 'ME', 'AF', 'TR', 'CIS', 'SEA'];
const EXPERIENCE_LEVELS = ['Beginner', 'Intermediate', 'Advanced', 'Expert'];
const COMMUNICATION_PREFS = ['Text', 'Voice', 'Both', 'None'];

interface GameForm {
  gameId: string;
  gameName: string;
  gameImageUrl: string | null;
  rank: string;
  role: string;
  region: string;
  experienceLevel: string;
  communicationPreference: string;
  lookingForTeam: boolean;
  note: string;
  inGameName: string;
}

const emptyForm = (game?: GameResponse): GameForm => ({
  gameId: game?.id ?? '',
  gameName: game?.name ?? '',
  gameImageUrl: game?.backgroundImageUrl ?? null,
  rank: '',
  role: '',
  region: 'EU',
  experienceLevel: 'Beginner',
  communicationPreference: 'Both',
  lookingForTeam: true,
  note: '',
  inGameName: '',
});

const fromExisting = (gp: GameProfileResponse): GameForm => ({
  gameId: gp.gameId,
  gameName: gp.gameName,
  gameImageUrl: gp.gameImageUrl,
  rank: gp.rank,
  role: gp.role ?? '',
  region: gp.region,
  experienceLevel: gp.experienceLevel,
  communicationPreference: gp.communicationPreference,
  lookingForTeam: gp.lookingForTeam,
  note: gp.note ?? '',
  inGameName: gp.inGameName ?? '',
});

export default function GameProfilesPage() {
  const { data: profile, isLoading } = useMyProfile();
  const addGameProfile = useAddGameProfile();
  const updateGameProfile = useUpdateGameProfile();
  const deleteGameProfile = useDeleteGameProfile();

  const [showAddModal, setShowAddModal] = useState(false);
  const [editingId, setEditingId] = useState<string | null>(null);
  const [form, setForm] = useState<GameForm>(emptyForm());
  const [gameQuery, setGameQuery] = useState('');
  const [selectedGame, setSelectedGame] = useState<GameResponse | null>(null);

  const { data: searchResults, isLoading: isSearching } = useGameSearch(gameQuery);

  const setField = useCallback(
    <K extends keyof GameForm>(field: K, value: GameForm[K]) => {
      setForm((prev) => ({ ...prev, [field]: value }));
    },
    [],
  );

  const handleSelectGame = (game: GameResponse) => {
    setSelectedGame(game);
    setForm(emptyForm(game));
    setGameQuery('');
  };

  const handleEdit = (gp: GameProfileResponse) => {
    setEditingId(gp.id);
    setForm(fromExisting(gp));
    setShowAddModal(true);
  };

  const handleDelete = async (id: string) => {
    await deleteGameProfile.mutateAsync(id);
  };

  const handleSubmit = async () => {
    if (editingId) {
      await updateGameProfile.mutateAsync({
        id: editingId,
        rank: form.rank,
        role: form.role || undefined,
        region: form.region,
        experienceLevel: form.experienceLevel,
        communicationPreference: form.communicationPreference,
        lookingForTeam: form.lookingForTeam,
        note: form.note || undefined,
        inGameName: form.inGameName || undefined,
      });
    } else {
      await addGameProfile.mutateAsync({
        gameId: form.gameId,
        rank: form.rank || 'Unranked',
        role: form.role || undefined,
        region: form.region,
        experienceLevel: form.experienceLevel,
        communicationPreference: form.communicationPreference,
        lookingForTeam: form.lookingForTeam,
        note: form.note || undefined,
        inGameName: form.inGameName || undefined,
      });
    }
    closeModal();
  };

  const closeModal = () => {
    setShowAddModal(false);
    setEditingId(null);
    setForm(emptyForm());
    setSelectedGame(null);
    setGameQuery('');
  };

  if (isLoading) {
    return (
      <div className="flex items-center justify-center py-32">
        <Spinner size="lg" />
      </div>
    );
  }

  const games = profile?.games ?? [];

  return (
    <AnimatedPage>
      <div className="mx-auto max-w-3xl space-y-6">
        <div className="flex items-center justify-between">
          <div>
            <h1 className="text-2xl font-bold text-foreground">Game Profiles</h1>
            <p className="mt-1 text-foreground-muted">
              Manage your in-game profiles, ranks, and preferences
            </p>
          </div>
          <Button
            onClick={() => {
              setEditingId(null);
              setForm(emptyForm());
              setShowAddModal(true);
            }}
          >
            Add Game
          </Button>
        </div>

        {games.length > 0 ? (
          <motion.div
            variants={staggerContainer}
            initial="hidden"
            animate="show"
            className="space-y-4"
          >
            {games.map((gp) => (
              <motion.div key={gp.id} variants={staggerItem}>
                <Card className="hover:border-primary/30 transition-colors">
                  <div className="flex items-start gap-4">
                    {gp.gameImageUrl ? (
                      <img
                        src={gp.gameImageUrl}
                        alt={gp.gameName}
                        className="w-16 h-16 rounded-lg object-cover"
                      />
                    ) : (
                      <div className="w-16 h-16 rounded-lg bg-border flex items-center justify-center text-2xl">
                        🎮
                      </div>
                    )}
                    <div className="flex-1 min-w-0">
                      <div className="flex items-center justify-between">
                        <h3 className="text-lg font-semibold text-foreground truncate">
                          {gp.gameName}
                        </h3>
                        <div className="flex gap-2 ml-2 shrink-0">
                          <Button variant="ghost" size="sm" onClick={() => handleEdit(gp)}>
                            Edit
                          </Button>
                          <Button
                            variant="danger"
                            size="sm"
                            onClick={() => handleDelete(gp.id)}
                            isLoading={deleteGameProfile.isPending}
                          >
                            Remove
                          </Button>
                        </div>
                      </div>
                      {gp.inGameName && (
                        <p className="text-sm text-foreground-muted">IGN: {gp.inGameName}</p>
                      )}
                      <div className="mt-2 flex flex-wrap gap-2">
                        <Badge variant="primary">{gp.rank}</Badge>
                        <Badge>{gp.experienceLevel}</Badge>
                        <Badge>{gp.region}</Badge>
                        <Badge>{gp.communicationPreference}</Badge>
                        {gp.lookingForTeam && <Badge variant="accent">LFT</Badge>}
                        {gp.hoursPlayed != null && <Badge>{gp.hoursPlayed}h</Badge>}
                      </div>
                      {gp.note && (
                        <p className="mt-2 text-sm text-foreground-muted italic">{gp.note}</p>
                      )}
                    </div>
                  </div>
                </Card>
              </motion.div>
            ))}
          </motion.div>
        ) : (
          <Card className="text-center py-12">
            <div className="text-5xl mb-3">🎮</div>
            <h3 className="text-lg font-semibold text-foreground">No game profiles yet</h3>
            <p className="mt-1 text-foreground-muted">
              Add your first game to let teammates know what you play
            </p>
            <Button
              className="mt-4"
              onClick={() => {
                setEditingId(null);
                setForm(emptyForm());
                setShowAddModal(true);
              }}
            >
              Add Your First Game
            </Button>
          </Card>
        )}
      </div>

      <Modal isOpen={showAddModal} onClose={closeModal} title={editingId ? 'Edit Game' : 'Add Game'}>
        <div className="space-y-4">
          {!editingId && !form.gameId && (
            <div className="relative">
              <Input
                value={gameQuery}
                onChange={(e) => setGameQuery(e.target.value)}
                placeholder="Search for a game..."
              />
              <AnimatePresence>
                {gameQuery.length >= 2 && (
                  <motion.div
                    initial={{ opacity: 0, y: -5 }}
                    animate={{ opacity: 1, y: 0 }}
                    exit={{ opacity: 0, y: -5 }}
                    className="absolute z-20 top-full left-0 right-0 mt-1 bg-surface border border-border rounded-lg shadow-lg max-h-48 overflow-y-auto"
                  >
                    {isSearching ? (
                      <div className="flex justify-center p-4">
                        <Spinner size="sm" />
                      </div>
                    ) : searchResults && searchResults.length > 0 ? (
                      searchResults.map((game) => (
                        <button
                          key={game.id}
                          className="w-full flex items-center gap-3 p-3 hover:bg-surface-hover transition-colors text-left"
                          onClick={() => handleSelectGame(game)}
                        >
                          {game.backgroundImageUrl ? (
                            <img
                              src={game.backgroundImageUrl}
                              alt={game.name}
                              className="w-8 h-8 rounded object-cover"
                            />
                          ) : (
                            <div className="w-8 h-8 rounded bg-border" />
                          )}
                          <span className="text-sm text-foreground">{game.name}</span>
                        </button>
                      ))
                    ) : (
                      <p className="p-4 text-sm text-foreground-muted text-center">
                        No games found
                      </p>
                    )}
                  </motion.div>
                )}
              </AnimatePresence>
            </div>
          )}

          {(form.gameId || editingId) && (
            <>
              <div className="flex items-center gap-3 p-3 bg-background rounded-lg border border-border">
                {form.gameImageUrl ? (
                  <img
                    src={form.gameImageUrl}
                    alt={form.gameName}
                    className="w-10 h-10 rounded object-cover"
                  />
                ) : (
                  <div className="w-10 h-10 rounded bg-border" />
                )}
                <span className="font-medium text-foreground">{form.gameName}</span>
                {!editingId && (
                  <Button
                    variant="ghost"
                    size="sm"
                    className="ml-auto"
                    onClick={() => {
                      setForm(emptyForm());
                      setSelectedGame(null);
                    }}
                  >
                    Change
                  </Button>
                )}
              </div>

              <div className="grid grid-cols-2 gap-3">
                <Input
                  label="In-Game Name"
                  value={form.inGameName}
                  onChange={(e) => setField('inGameName', e.target.value)}
                  placeholder="Your IGN"
                />
                <Input
                  label="Rank"
                  value={form.rank}
                  onChange={(e) => setField('rank', e.target.value)}
                  placeholder="e.g. Gold, Diamond"
                />
                <Select
                  label="Region"
                  value={form.region}
                  onChange={(e) => setField('region', e.target.value)}
                  options={REGIONS.map((r) => ({ value: r, label: r }))}
                />
                <Select
                  label="Experience"
                  value={form.experienceLevel}
                  onChange={(e) => setField('experienceLevel', e.target.value)}
                  options={EXPERIENCE_LEVELS.map((l) => ({ value: l, label: l }))}
                />
                <Select
                  label="Communication"
                  value={form.communicationPreference}
                  onChange={(e) => setField('communicationPreference', e.target.value)}
                  options={COMMUNICATION_PREFS.map((c) => ({ value: c, label: c }))}
                />
                <Input
                  label="Role"
                  value={form.role}
                  onChange={(e) => setField('role', e.target.value)}
                  placeholder="e.g. Support, DPS"
                />
              </div>

              <Input
                label="Note"
                value={form.note}
                onChange={(e) => setField('note', e.target.value)}
                placeholder="Any additional info..."
              />

              <label className="flex items-center gap-2 cursor-pointer">
                <input
                  type="checkbox"
                  checked={form.lookingForTeam}
                  onChange={(e) => setField('lookingForTeam', e.target.checked)}
                  className="rounded border-border"
                />
                <span className="text-sm text-foreground">Looking for team</span>
              </label>

              <div className="flex justify-end gap-2 pt-2">
                <Button variant="ghost" onClick={closeModal}>
                  Cancel
                </Button>
                <Button
                  onClick={handleSubmit}
                  isLoading={addGameProfile.isPending || updateGameProfile.isPending}
                >
                  {editingId ? 'Save Changes' : 'Add Game'}
                </Button>
              </div>
            </>
          )}
        </div>
      </Modal>
    </AnimatedPage>
  );
}
