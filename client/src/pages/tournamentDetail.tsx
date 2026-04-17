import { useState } from 'react';
import { useParams, Link } from 'react-router-dom';
import { motion } from 'framer-motion';
import {
  Button,
  Badge,
  Spinner,
  Modal,
  Select,
  AnimatedPage,
  staggerContainer,
  staggerItem,
  useToast,
} from '@/components/ui';
import { useAuthStore } from '@/stores/authStore';
import {
  useTournamentDetail,
  useJoinTournament,
  useLeaveTournament,
  useGenerateBracket,
  useReportMatchResult,
} from '@/features/tournaments/hooks';
import type { TournamentEntryResponse, TournamentMatchResponse } from '@/features/tournaments/types';
import { BracketView } from '@/components/tournaments/bracketView';
import { cn } from '@/lib/cn';

const STATUS_BADGE_MAP: Record<string, { variant: 'default' | 'primary' | 'accent' | 'danger' | 'warning' | 'success'; label: string }> = {
  Registration: { variant: 'primary', label: 'Registration Open' },
  InProgress: { variant: 'warning', label: 'In Progress' },
  Completed: { variant: 'success', label: 'Completed' },
  Cancelled: { variant: 'danger', label: 'Cancelled' },
};

const FORMAT_LABELS: Record<string, string> = {
  SingleElimination: 'Single Elimination',
  DoubleElimination: 'Double Elimination',
  '0': 'Single Elimination',
  '1': 'Double Elimination',
};

function InfoCard({ icon, label, value, delay }: { icon: string; label: string; value: string; delay: number }) {
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

function ParticipantRow({ entry, index }: { entry: TournamentEntryResponse; index: number }) {
  return (
    <motion.div
      variants={staggerItem}
      custom={index}
      className={cn(
        'flex items-center gap-3 rounded-lg px-3 py-2 transition-colors',
        entry.isEliminated ? 'opacity-50' : 'hover:bg-surface-hover/40',
      )}
    >
      <span className="flex h-7 w-7 items-center justify-center rounded-full bg-surface-hover/60 text-xs font-bold text-foreground-subtle">
        #{entry.seed}
      </span>
      <div className="flex min-w-0 flex-1 items-center gap-2">
        <span className="truncate text-sm font-medium text-foreground">{entry.participantName}</span>
        {entry.isEliminated && (
          <Badge variant="danger" className="text-[10px]">Eliminated</Badge>
        )}
        {entry.placement > 0 && entry.placement <= 3 && (
          <Badge variant="warning" className="text-[10px]">
            {entry.placement === 1 ? '🥇' : entry.placement === 2 ? '🥈' : '🥉'} #{entry.placement}
          </Badge>
        )}
      </div>
      {entry.earnedBadges.length > 0 && (
        <div className="flex gap-1">
          {entry.earnedBadges.map((b) => (
            <Badge key={b} variant="accent" className="text-[10px]">{b}</Badge>
          ))}
        </div>
      )}
    </motion.div>
  );
}

function ReportResultModal({
  isOpen,
  onClose,
  pendingMatches,
}: {
  isOpen: boolean;
  onClose: () => void;
  pendingMatches: TournamentMatchResponse[];
}) {
  const { addToast } = useToast();
  const reportMutation = useReportMatchResult();
  const [selectedMatchId, setSelectedMatchId] = useState('');
  const [selectedWinnerId, setSelectedWinnerId] = useState('');

  const selectedMatch = pendingMatches.find((m) => m.id === selectedMatchId);

  const winnerOptions = selectedMatch
    ? [
        selectedMatch.participant1Id && { value: selectedMatch.participant1Id, label: selectedMatch.participant1Name ?? 'Player 1' },
        selectedMatch.participant2Id && { value: selectedMatch.participant2Id, label: selectedMatch.participant2Name ?? 'Player 2' },
      ].filter(Boolean) as { value: string; label: string }[]
    : [];

  const handleReport = () => {
    if (!selectedMatchId || !selectedWinnerId) {
      addToast({ title: 'Missing selection', message: 'Pick both a match and winner.', type: 'warning' });
      return;
    }
    reportMutation.mutate(
      { matchId: selectedMatchId, winnerId: selectedWinnerId },
      {
        onSuccess: () => {
          addToast({ title: 'Result reported', message: 'Match result saved.', type: 'success' });
          setSelectedMatchId('');
          setSelectedWinnerId('');
          onClose();
        },
        onError: () => {
          addToast({ title: 'Error', message: 'Failed to report result.', type: 'error' });
        },
      },
    );
  };

  return (
    <Modal isOpen={isOpen} onClose={onClose} title="Report Match Result">
      <div className="space-y-4">
        <Select
          label="Select Match"
          placeholder="Choose a match..."
          value={selectedMatchId}
          onChange={(e) => { setSelectedMatchId(e.target.value); setSelectedWinnerId(''); }}
          options={pendingMatches.map((m) => ({
            value: m.id,
            label: `Match ${m.matchNumber}: ${m.participant1Name ?? 'BYE'} vs ${m.participant2Name ?? 'BYE'}`,
          }))}
        />
        {selectedMatch && (
          <Select
            label="Winner"
            placeholder="Select winner..."
            value={selectedWinnerId}
            onChange={(e) => setSelectedWinnerId(e.target.value)}
            options={winnerOptions}
          />
        )}
        <div className="flex justify-end gap-3 pt-2">
          <Button variant="ghost" onClick={onClose}>Cancel</Button>
          <Button onClick={handleReport} isLoading={reportMutation.isPending} disabled={!selectedMatchId || !selectedWinnerId}>
            Report Result
          </Button>
        </div>
      </div>
    </Modal>
  );
}

export default function TournamentDetailPage() {
  const { tournamentId } = useParams<{ tournamentId: string }>();
  const { data: tournament, isLoading, error } = useTournamentDetail(tournamentId);
  const user = useAuthStore((s) => s.user);
  const { addToast } = useToast();

  const joinMutation = useJoinTournament();
  const leaveMutation = useLeaveTournament();
  const bracketMutation = useGenerateBracket();

  const [showReportModal, setShowReportModal] = useState(false);
  const [activeTab, setActiveTab] = useState<'bracket' | 'participants'>('bracket');

  if (isLoading) {
    return (
      <div className="flex min-h-[60vh] items-center justify-center">
        <Spinner size="lg" />
      </div>
    );
  }

  if (error || !tournament) {
    return (
      <AnimatedPage>
        <div className="flex flex-col items-center py-32 text-center">
          <motion.div initial={{ scale: 0 }} animate={{ scale: 1 }} transition={{ type: 'spring', bounce: 0.5 }} className="text-7xl">
            🏆
          </motion.div>
          <h2 className="mt-6 text-2xl font-bold text-foreground">Tournament not found</h2>
          <p className="mt-2 text-foreground-muted">This tournament may have been removed.</p>
          <Link to="/tournaments" className="mt-8">
            <Button size="lg">Browse Tournaments</Button>
          </Link>
        </div>
      </AnimatedPage>
    );
  }

  const statusMeta = STATUS_BADGE_MAP[tournament.status] ?? { variant: 'default' as const, label: tournament.status };
  const isOrganizer = user?.id === tournament.organizerId;
  const isRegistrationOpen = tournament.status === 'Registration';
  const isInProgress = tournament.status === 'InProgress';
  const deadlineStr = new Date(tournament.registrationDeadline).toLocaleDateString('en-US', { month: 'long', day: 'numeric', year: 'numeric' });
  const startStr = tournament.startsAt
    ? new Date(tournament.startsAt).toLocaleDateString('en-US', { month: 'long', day: 'numeric', year: 'numeric' })
    : 'TBD';

  const pendingMatches = tournament.matches.filter(
    (m) => m.status.toLowerCase() !== 'completed' && m.participant1Id && m.participant2Id,
  );

  const handleJoin = () => {
    joinMutation.mutate(tournament.id, {
      onSuccess: () => addToast({ title: 'Joined!', message: `You're in ${tournament.name}.`, type: 'success' }),
      onError: () => addToast({ title: 'Error', message: 'Failed to join tournament.', type: 'error' }),
    });
  };

  const handleLeave = () => {
    leaveMutation.mutate(tournament.id, {
      onSuccess: () => addToast({ title: 'Left tournament', message: 'You have left the tournament.', type: 'info' }),
      onError: () => addToast({ title: 'Error', message: 'Failed to leave tournament.', type: 'error' }),
    });
  };

  const handleGenerateBracket = () => {
    bracketMutation.mutate(tournament.id, {
      onSuccess: () => addToast({ title: 'Bracket generated', message: 'Matches are ready!', type: 'success' }),
      onError: () => addToast({ title: 'Error', message: 'Failed to generate bracket.', type: 'error' }),
    });
  };

  return (
    <AnimatedPage>
      <div className="mx-auto max-w-6xl px-4 py-6 lg:px-8">
        {/* Back link */}
        <motion.div initial={{ opacity: 0, x: -12 }} animate={{ opacity: 1, x: 0 }} className="mb-6">
          <Link
            to="/tournaments"
            className="inline-flex items-center gap-1.5 rounded-full bg-surface/40 px-3 py-1.5 text-xs font-medium text-foreground-muted backdrop-blur-sm transition-colors hover:bg-surface/60 hover:text-foreground"
          >
            <svg className="h-3.5 w-3.5" fill="none" stroke="currentColor" viewBox="0 0 24 24" strokeWidth={2.5}>
              <path strokeLinecap="round" strokeLinejoin="round" d="M15.75 19.5L8.25 12l7.5-7.5" />
            </svg>
            Tournaments
          </Link>
        </motion.div>

        {/* Header */}
        <motion.div
          initial={{ opacity: 0, y: -16 }}
          animate={{ opacity: 1, y: 0 }}
          className="mb-6 overflow-hidden rounded-2xl border border-border/50 bg-linear-to-br from-surface/80 to-surface/40 p-6 backdrop-blur-sm"
        >
          <div className="flex flex-col gap-4 sm:flex-row sm:items-start sm:justify-between">
            <div className="min-w-0">
              <div className="mb-2 flex flex-wrap items-center gap-2">
                <Badge variant={statusMeta.variant}>{statusMeta.label}</Badge>
                <Badge variant="accent">{FORMAT_LABELS[tournament.format] ?? tournament.format}</Badge>
              </div>
              <h1 className="text-3xl font-extrabold tracking-tight text-foreground lg:text-4xl">
                {tournament.name}
              </h1>
              <p className="mt-2 flex items-center gap-2 text-sm text-foreground-muted">
                <span>🎮 {tournament.gameName}</span>
                <span className="text-foreground-subtle">·</span>
                <span>Organized by {tournament.organizerUsername}</span>
              </p>
              {tournament.description && (
                <p className="mt-3 max-w-2xl text-sm leading-relaxed text-foreground-muted">{tournament.description}</p>
              )}
              {tournament.prizeBadges.length > 0 && (
                <div className="mt-3 flex flex-wrap gap-1.5">
                  {tournament.prizeBadges.map((p) => (
                    <Badge key={p} variant="warning">🏆 {p}</Badge>
                  ))}
                </div>
              )}
            </div>

            {/* Action buttons */}
            <div className="flex shrink-0 flex-wrap gap-2">
              {isRegistrationOpen && !tournament.isParticipant && (
                <Button onClick={handleJoin} isLoading={joinMutation.isPending}>
                  Join Tournament
                </Button>
              )}
              {isRegistrationOpen && tournament.isParticipant && (
                <Button variant="danger" onClick={handleLeave} isLoading={leaveMutation.isPending}>
                  Leave
                </Button>
              )}
              {isOrganizer && isRegistrationOpen && tournament.matches.length === 0 && (
                <Button variant="secondary" onClick={handleGenerateBracket} isLoading={bracketMutation.isPending}>
                  Generate Bracket
                </Button>
              )}
              {isOrganizer && isInProgress && pendingMatches.length > 0 && (
                <Button variant="secondary" onClick={() => setShowReportModal(true)}>
                  Report Result
                </Button>
              )}
            </div>
          </div>
        </motion.div>

        {/* Info Cards */}
        <div className="mb-6 grid grid-cols-2 gap-3 sm:grid-cols-4">
          <InfoCard icon="👥" label="Participants" value={`${tournament.currentParticipants} / ${tournament.maxParticipants}`} delay={0.15} />
          <InfoCard icon="📅" label="Deadline" value={deadlineStr} delay={0.2} />
          <InfoCard icon="🚀" label="Starts" value={startStr} delay={0.25} />
          <InfoCard icon="🔄" label="Round" value={tournament.totalRounds > 0 ? `${tournament.currentRound} / ${tournament.totalRounds}` : '—'} delay={0.3} />
        </div>

        {/* Tabs */}
        <motion.div
          initial={{ opacity: 0 }}
          animate={{ opacity: 1 }}
          transition={{ delay: 0.2 }}
          className="mb-6 flex gap-1 rounded-xl bg-surface-hover/40 p-1"
        >
          {(['bracket', 'participants'] as const).map((tab) => (
            <button
              key={tab}
              onClick={() => setActiveTab(tab)}
              className={cn(
                'flex-1 rounded-lg px-4 py-2 text-sm font-semibold capitalize transition-all',
                activeTab === tab
                  ? 'bg-surface text-foreground shadow-sm'
                  : 'text-foreground-muted hover:text-foreground',
              )}
            >
              {tab === 'bracket' ? '🏟️ Bracket' : '👥 Participants'}
            </button>
          ))}
        </motion.div>

        {/* Tab Content */}
        {activeTab === 'bracket' ? (
          <motion.div
            initial={{ opacity: 0, y: 12 }}
            animate={{ opacity: 1, y: 0 }}
            transition={{ delay: 0.1 }}
            className="overflow-hidden rounded-2xl border border-border/50 bg-linear-to-br from-surface/80 to-surface/40 p-6 backdrop-blur-sm"
          >
            <h2 className="mb-5 text-sm font-semibold uppercase tracking-widest text-foreground-subtle">Tournament Bracket</h2>
            <BracketView
              matches={tournament.matches}
              totalRounds={tournament.totalRounds}
              currentRound={tournament.currentRound}
            />
          </motion.div>
        ) : (
          <motion.div
            initial={{ opacity: 0, y: 12 }}
            animate={{ opacity: 1, y: 0 }}
            transition={{ delay: 0.1 }}
            className="overflow-hidden rounded-2xl border border-border/50 bg-linear-to-br from-surface/80 to-surface/40 p-4 backdrop-blur-sm"
          >
            <h2 className="mb-4 px-2 text-sm font-semibold uppercase tracking-widest text-foreground-subtle">
              Participants ({tournament.entries.length})
            </h2>
            {tournament.entries.length === 0 ? (
              <div className="py-12 text-center">
                <span className="text-4xl">👤</span>
                <p className="mt-3 text-sm text-foreground-muted">No participants yet. Be the first to join!</p>
              </div>
            ) : (
              <motion.div variants={staggerContainer} initial="hidden" animate="show" className="space-y-1">
                {tournament.entries
                  .slice()
                  .sort((a, b) => a.seed - b.seed)
                  .map((entry, i) => (
                    <ParticipantRow key={entry.id} entry={entry} index={i} />
                  ))}
              </motion.div>
            )}
          </motion.div>
        )}
      </div>

      <ReportResultModal
        isOpen={showReportModal}
        onClose={() => setShowReportModal(false)}
        pendingMatches={pendingMatches}
      />
    </AnimatedPage>
  );
}
