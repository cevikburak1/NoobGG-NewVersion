import { motion } from 'framer-motion';
import type { TournamentMatchResponse } from '@/features/tournaments/types';
import { Badge } from '@/components/ui';
import { cn } from '@/lib/cn';

interface BracketViewProps {
  matches: TournamentMatchResponse[];
  totalRounds: number;
  currentRound: number;
}

const MATCH_HEIGHT = 72;
const MATCH_GAP = 24;
const ROUND_GAP = 64;

function getMatchStatusBadge(status: string) {
  switch (status.toLowerCase()) {
    case 'completed':
      return <Badge variant="success">Done</Badge>;
    case 'inprogress':
      return <Badge variant="warning">Live</Badge>;
    case 'pending':
      return <Badge variant="default">Pending</Badge>;
    default:
      return <Badge variant="default">{status}</Badge>;
  }
}

function MatchCard({
  match,
  isCurrentRound,
  index,
}: {
  match: TournamentMatchResponse;
  isCurrentRound: boolean;
  index: number;
}) {
  const isComplete = match.status.toLowerCase() === 'completed';
  const p1Won = isComplete && match.winnerId === match.participant1Id;
  const p2Won = isComplete && match.winnerId === match.participant2Id;

  return (
    <motion.div
      initial={{ opacity: 0, scale: 0.9 }}
      animate={{ opacity: 1, scale: 1 }}
      transition={{ delay: 0.1 + index * 0.04, duration: 0.35 }}
      className={cn(
        'relative flex w-56 flex-col overflow-hidden rounded-xl border backdrop-blur-sm transition-shadow',
        isCurrentRound
          ? 'border-primary/50 bg-surface/90 shadow-[0_0_20px_rgba(124,58,237,0.15)]'
          : 'border-border/50 bg-surface/70',
      )}
    >
      <div className="flex items-center justify-between border-b border-border/30 px-3 py-1.5">
        <span className="text-[10px] font-medium uppercase tracking-wider text-foreground-subtle">
          Match {match.matchNumber}
        </span>
        {getMatchStatusBadge(match.status)}
      </div>

      <div className="flex flex-col divide-y divide-border/20">
        <div
          className={cn(
            'flex items-center gap-2 px-3 py-2 transition-colors',
            p1Won && 'bg-success/5',
            isComplete && !p1Won && match.participant1Id && 'opacity-40',
          )}
        >
          {p1Won && <span className="text-xs text-success">▶</span>}
          <span
            className={cn(
              'flex-1 truncate text-sm',
              p1Won ? 'font-bold text-success' : 'text-foreground',
            )}
          >
            {match.participant1Name ?? (
              <span className="italic text-foreground-subtle">BYE</span>
            )}
          </span>
        </div>

        <div
          className={cn(
            'flex items-center gap-2 px-3 py-2 transition-colors',
            p2Won && 'bg-success/5',
            isComplete && !p2Won && match.participant2Id && 'opacity-40',
          )}
        >
          {p2Won && <span className="text-xs text-success">▶</span>}
          <span
            className={cn(
              'flex-1 truncate text-sm',
              p2Won ? 'font-bold text-success' : 'text-foreground',
            )}
          >
            {match.participant2Name ?? (
              <span className="italic text-foreground-subtle">BYE</span>
            )}
          </span>
        </div>
      </div>
    </motion.div>
  );
}

function ConnectorLines({
  matchCount,
  roundIndex,
}: {
  matchCount: number;
  roundIndex: number;
}) {
  const lines: React.ReactNode[] = [];
  const pairCount = Math.ceil(matchCount / 2);
  const spacing = MATCH_HEIGHT + MATCH_GAP;

  for (let i = 0; i < pairCount; i++) {
    const topY = i * 2 * spacing + MATCH_HEIGHT / 2;
    const bottomY = (i * 2 + 1) * spacing + MATCH_HEIGHT / 2;
    const midY = (topY + bottomY) / 2;

    lines.push(
      <g key={`${roundIndex}-${i}`}>
        <line
          x1="0"
          y1={topY}
          x2="20"
          y2={topY}
          stroke="currentColor"
          strokeWidth="1.5"
          className="text-border/60"
        />
        <line
          x1="0"
          y1={bottomY}
          x2="20"
          y2={bottomY}
          stroke="currentColor"
          strokeWidth="1.5"
          className="text-border/60"
        />
        <line
          x1="20"
          y1={topY}
          x2="20"
          y2={bottomY}
          stroke="currentColor"
          strokeWidth="1.5"
          className="text-border/60"
        />
        <line
          x1="20"
          y1={midY}
          x2={ROUND_GAP - 8}
          y2={midY}
          stroke="currentColor"
          strokeWidth="1.5"
          className="text-border/60"
        />
      </g>,
    );
  }

  const totalHeight = matchCount * spacing;
  return (
    <svg
      width={ROUND_GAP}
      height={totalHeight}
      className="shrink-0"
      style={{ minHeight: totalHeight }}
    >
      {lines}
    </svg>
  );
}

export function BracketView({ matches, totalRounds, currentRound }: BracketViewProps) {
  const roundGroups: TournamentMatchResponse[][] = [];
  for (let r = 1; r <= totalRounds; r++) {
    roundGroups.push(
      matches
        .filter((m) => m.round === r)
        .sort((a, b) => a.matchNumber - b.matchNumber),
    );
  }

  if (matches.length === 0) {
    return (
      <motion.div
        initial={{ opacity: 0 }}
        animate={{ opacity: 1 }}
        className="flex flex-col items-center py-12 text-center"
      >
        <span className="text-5xl">🏗️</span>
        <p className="mt-4 text-sm font-medium text-foreground-muted">
          Bracket not yet generated
        </p>
      </motion.div>
    );
  }

  const roundLabels = roundGroups.map((_, i) => {
    if (i === totalRounds - 1) return 'Final';
    if (i === totalRounds - 2) return 'Semifinal';
    return `Round ${i + 1}`;
  });

  return (
    <div className="overflow-x-auto pb-4">
      <div className="flex items-start gap-0" style={{ minWidth: roundGroups.length * 300 }}>
        {roundGroups.map((roundMatches, roundIdx) => (
          <div key={roundIdx} className="flex items-start">
            <div className="flex flex-col">
              <motion.div
                initial={{ opacity: 0, y: -8 }}
                animate={{ opacity: 1, y: 0 }}
                transition={{ delay: roundIdx * 0.1 }}
                className="mb-4 text-center"
              >
                <span
                  className={cn(
                    'inline-block rounded-full px-3 py-1 text-xs font-bold uppercase tracking-wider',
                    roundIdx + 1 === currentRound
                      ? 'bg-primary/20 text-primary'
                      : 'text-foreground-subtle',
                  )}
                >
                  {roundLabels[roundIdx]}
                </span>
              </motion.div>

              <div
                className="flex flex-col"
                style={{ gap: MATCH_GAP }}
              >
                {roundMatches.map((match, mIdx) => (
                  <MatchCard
                    key={match.id}
                    match={match}
                    isCurrentRound={match.round === currentRound}
                    index={roundIdx * 4 + mIdx}
                  />
                ))}
              </div>
            </div>

            {roundIdx < roundGroups.length - 1 && (
              <div className="mt-10">
                <ConnectorLines
                  matchCount={roundMatches.length}
                  roundIndex={roundIdx}
                />
              </div>
            )}
          </div>
        ))}
      </div>
    </div>
  );
}
