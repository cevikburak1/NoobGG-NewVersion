import { useState } from 'react';
import { Link } from 'react-router-dom';
import { motion } from 'framer-motion';
import {
  Button,
  Input,
  Textarea,
  Select,
  Modal,
  Badge,
  Spinner,
  AnimatedPage,
  staggerContainer,
  staggerItem,
  useToast,
} from '@/components/ui';
import { useTournaments, useCreateTournament } from '@/features/tournaments/hooks';
import type { CreateTournamentPayload, TournamentListItemResponse } from '@/features/tournaments/types';

const STATUS_FILTERS = ['All', 'Registration', 'InProgress', 'Completed'] as const;

const STATUS_BADGE_MAP: Record<string, { variant: 'default' | 'primary' | 'accent' | 'danger' | 'warning' | 'success'; label: string }> = {
  Registration: { variant: 'primary', label: 'Registration' },
  InProgress: { variant: 'warning', label: 'In Progress' },
  Completed: { variant: 'success', label: 'Completed' },
  Cancelled: { variant: 'danger', label: 'Cancelled' },
};

const FORMAT_LABELS: Record<string, string> = {
  SingleElimination: 'Single Elim',
  DoubleElimination: 'Double Elim',
  '0': 'Single Elim',
  '1': 'Double Elim',
};

function TournamentCard({ tournament, index }: { tournament: TournamentListItemResponse; index: number }) {
  const statusMeta = STATUS_BADGE_MAP[tournament.status] ?? { variant: 'default' as const, label: tournament.status };
  const deadlineDate = new Date(tournament.registrationDeadline);
  const isPastDeadline = deadlineDate < new Date();
  const formattedDeadline = deadlineDate.toLocaleDateString('en-US', { month: 'short', day: 'numeric', year: 'numeric' });

  return (
    <motion.div variants={staggerItem} custom={index}>
      <Link to={`/tournaments/${tournament.id}`} className="block">
        <motion.div
          whileHover={{ y: -4, scale: 1.01 }}
          transition={{ type: 'spring', stiffness: 300, damping: 25 }}
          className="group relative overflow-hidden rounded-2xl border border-border/50 bg-surface/80 backdrop-blur-sm transition-shadow hover:shadow-xl hover:shadow-primary/5"
        >
          <div className="absolute inset-0 bg-linear-to-br from-primary/5 via-transparent to-accent/5 opacity-0 transition-opacity group-hover:opacity-100" />

          <div className="relative p-5">
            <div className="mb-3 flex items-start justify-between gap-2">
              <div className="min-w-0 flex-1">
                <h3 className="truncate text-base font-bold text-foreground group-hover:text-primary transition-colors">
                  {tournament.name}
                </h3>
                <p className="mt-0.5 text-xs font-medium text-foreground-muted">{tournament.gameName}</p>
              </div>
              <Badge variant={statusMeta.variant}>{statusMeta.label}</Badge>
            </div>

            <div className="mb-4 flex flex-wrap gap-1.5">
              <Badge variant="accent" className="text-[10px]">
                {FORMAT_LABELS[tournament.format] ?? tournament.format}
              </Badge>
              {tournament.prizeBadges.map((prize) => (
                <Badge key={prize} variant="warning" className="text-[10px]">
                  🏆 {prize}
                </Badge>
              ))}
            </div>

            <div className="grid grid-cols-2 gap-3">
              <div className="flex items-center gap-2 rounded-lg bg-surface-hover/40 px-2.5 py-1.5">
                <span className="text-sm">👥</span>
                <div>
                  <div className="text-[10px] font-medium uppercase tracking-wider text-foreground-subtle">Players</div>
                  <div className="text-xs font-semibold text-foreground">
                    {tournament.currentParticipants}/{tournament.maxParticipants}
                  </div>
                </div>
              </div>

              <div className="flex items-center gap-2 rounded-lg bg-surface-hover/40 px-2.5 py-1.5">
                <span className="text-sm">📅</span>
                <div>
                  <div className="text-[10px] font-medium uppercase tracking-wider text-foreground-subtle">Deadline</div>
                  <div className={`text-xs font-semibold ${isPastDeadline ? 'text-foreground-subtle' : 'text-foreground'}`}>
                    {formattedDeadline}
                  </div>
                </div>
              </div>
            </div>

            <div className="mt-3 flex items-center justify-between">
              <span className="text-[10px] text-foreground-subtle">
                by {tournament.organizerUsername}
              </span>
              <div className="h-1.5 w-full max-w-[80px] overflow-hidden rounded-full bg-surface-hover/60">
                <motion.div
                  initial={{ width: 0 }}
                  animate={{ width: `${(tournament.currentParticipants / tournament.maxParticipants) * 100}%` }}
                  transition={{ delay: 0.3 + index * 0.05, duration: 0.6 }}
                  className="h-full rounded-full bg-primary/60"
                />
              </div>
            </div>
          </div>
        </motion.div>
      </Link>
    </motion.div>
  );
}

function CreateTournamentModal({ isOpen, onClose }: { isOpen: boolean; onClose: () => void }) {
  const { addToast } = useToast();
  const createMutation = useCreateTournament();
  const [form, setForm] = useState({
    name: '',
    description: '',
    gameId: '',
    format: '0',
    maxParticipants: '8',
    registrationDeadline: '',
    prizeBadges: '',
  });

  const updateField = (field: string, value: string) =>
    setForm((prev) => ({ ...prev, [field]: value }));

  const handleSubmit = () => {
    if (!form.name.trim() || !form.gameId.trim() || !form.registrationDeadline) {
      addToast({ title: 'Missing fields', message: 'Name, Game ID, and deadline are required.', type: 'warning' });
      return;
    }

    const payload: CreateTournamentPayload = {
      name: form.name.trim(),
      description: form.description.trim() || undefined,
      gameId: form.gameId.trim(),
      format: Number(form.format),
      maxParticipants: Number(form.maxParticipants) || 8,
      registrationDeadline: new Date(form.registrationDeadline).toISOString(),
      prizeBadges: form.prizeBadges
        .split(',')
        .map((b) => b.trim())
        .filter(Boolean),
    };

    createMutation.mutate(payload, {
      onSuccess: () => {
        addToast({ title: 'Tournament created', message: `${payload.name} is live!`, type: 'success' });
        onClose();
        setForm({ name: '', description: '', gameId: '', format: '0', maxParticipants: '8', registrationDeadline: '', prizeBadges: '' });
      },
      onError: () => {
        addToast({ title: 'Error', message: 'Failed to create tournament.', type: 'error' });
      },
    });
  };

  return (
    <Modal isOpen={isOpen} onClose={onClose} title="Create Tournament" className="max-w-lg">
      <div className="space-y-4">
        <Input
          label="Tournament Name"
          placeholder="e.g. Noob Weekly Championship"
          value={form.name}
          onChange={(e) => updateField('name', e.target.value)}
        />
        <Textarea
          label="Description"
          placeholder="Tournament details..."
          rows={3}
          value={form.description}
          onChange={(e) => updateField('description', e.target.value)}
        />
        <Input
          label="Game ID"
          placeholder="Enter the game identifier"
          value={form.gameId}
          onChange={(e) => updateField('gameId', e.target.value)}
        />
        <div className="grid grid-cols-2 gap-3">
          <Select
            label="Format"
            value={form.format}
            onChange={(e) => updateField('format', e.target.value)}
            options={[
              { value: '0', label: 'Single Elimination' },
              { value: '1', label: 'Double Elimination' },
            ]}
          />
          <Input
            label="Max Participants"
            type="number"
            min={2}
            max={128}
            value={form.maxParticipants}
            onChange={(e) => updateField('maxParticipants', e.target.value)}
          />
        </div>
        <Input
          label="Registration Deadline"
          type="datetime-local"
          value={form.registrationDeadline}
          onChange={(e) => updateField('registrationDeadline', e.target.value)}
        />
        <Input
          label="Prize Badges (comma-separated)"
          placeholder="Gold, Silver, Bronze"
          value={form.prizeBadges}
          onChange={(e) => updateField('prizeBadges', e.target.value)}
        />

        <div className="flex justify-end gap-3 pt-2">
          <Button variant="ghost" onClick={onClose}>Cancel</Button>
          <Button onClick={handleSubmit} isLoading={createMutation.isPending}>
            Create Tournament
          </Button>
        </div>
      </div>
    </Modal>
  );
}

export default function TournamentListPage() {
  const [gameFilter, setGameFilter] = useState('');
  const [statusFilter, setStatusFilter] = useState<string>('All');
  const [showCreate, setShowCreate] = useState(false);

  const { data, isLoading } = useTournaments({
    gameId: gameFilter || undefined,
    status: statusFilter === 'All' ? undefined : statusFilter,
  });

  const tournaments = data?.tournaments ?? [];

  return (
    <AnimatedPage>
      <div className="mx-auto max-w-6xl px-4 py-6 lg:px-8">
        {/* Header */}
        <motion.div
          initial={{ opacity: 0, y: -12 }}
          animate={{ opacity: 1, y: 0 }}
          className="mb-8 flex flex-col gap-4 sm:flex-row sm:items-center sm:justify-between"
        >
          <div>
            <h1 className="text-3xl font-extrabold tracking-tight text-foreground">Tournaments</h1>
            <p className="mt-1 text-sm text-foreground-muted">Compete, conquer, and climb the ranks</p>
          </div>
          <Button size="lg" onClick={() => setShowCreate(true)}>
            + Create Tournament
          </Button>
        </motion.div>

        {/* Filters */}
        <motion.div
          initial={{ opacity: 0, y: 8 }}
          animate={{ opacity: 1, y: 0 }}
          transition={{ delay: 0.1 }}
          className="mb-6 flex flex-col gap-4 sm:flex-row sm:items-center"
        >
          <div className="w-full sm:max-w-xs">
            <Input
              placeholder="Filter by Game ID..."
              value={gameFilter}
              onChange={(e) => setGameFilter(e.target.value)}
            />
          </div>

          <div className="flex flex-wrap gap-2">
            {STATUS_FILTERS.map((s) => (
              <button
                key={s}
                onClick={() => setStatusFilter(s)}
                className={`rounded-full px-3 py-1.5 text-xs font-semibold transition-all ${
                  statusFilter === s
                    ? 'bg-primary text-primary-foreground shadow-md shadow-primary/20'
                    : 'bg-surface-hover/60 text-foreground-muted hover:bg-surface-hover hover:text-foreground'
                }`}
              >
                {s === 'InProgress' ? 'In Progress' : s}
              </button>
            ))}
          </div>
        </motion.div>

        {/* Content */}
        {isLoading ? (
          <div className="flex min-h-[40vh] items-center justify-center">
            <Spinner size="lg" />
          </div>
        ) : tournaments.length === 0 ? (
          <motion.div
            initial={{ opacity: 0, scale: 0.9 }}
            animate={{ opacity: 1, scale: 1 }}
            className="flex flex-col items-center py-24 text-center"
          >
            <motion.span
              initial={{ rotate: -10 }}
              animate={{ rotate: [0, -10, 10, 0] }}
              transition={{ repeat: Infinity, repeatDelay: 3, duration: 0.6 }}
              className="text-7xl"
            >
              🏆
            </motion.span>
            <h2 className="mt-6 text-xl font-bold text-foreground">No tournaments found</h2>
            <p className="mt-2 max-w-sm text-sm text-foreground-muted">
              {statusFilter !== 'All'
                ? `No ${statusFilter.toLowerCase()} tournaments right now. Try a different filter.`
                : 'Be the first to create a tournament and rally the community!'}
            </p>
            <Button className="mt-6" onClick={() => setShowCreate(true)}>
              Create the First Tournament
            </Button>
          </motion.div>
        ) : (
          <motion.div
            variants={staggerContainer}
            initial="hidden"
            animate="show"
            className="grid gap-4 sm:grid-cols-2 lg:grid-cols-3"
          >
            {tournaments.map((t, i) => (
              <TournamentCard key={t.id} tournament={t} index={i} />
            ))}
          </motion.div>
        )}
      </div>

      <CreateTournamentModal isOpen={showCreate} onClose={() => setShowCreate(false)} />
    </AnimatedPage>
  );
}
