import { useState } from 'react';
import { Link } from 'react-router-dom';
import { motion } from 'framer-motion';
import { useLeaderboard } from '@/features/elo/hooks';
import { useGameBrowse } from '@/features/games/hooks';
import { useAuthStore } from '@/stores/authStore';
import type { LeaderboardEntry } from '@/features/elo/types';
import {
  Select, Button, AnimatedPage, Spinner,
  staggerContainer, staggerItem,
} from '@/components/ui';
import { RecordMatchModal } from '@/components/elo/recordMatchModal';

const tierConfig: Record<string, { bg: string; text: string; border: string }> = {
  Bronze:      { bg: 'bg-amber-700/30',  text: 'text-amber-400',   border: 'border-amber-600' },
  Silver:      { bg: 'bg-gray-500/30',   text: 'text-gray-300',    border: 'border-gray-500' },
  Gold:        { bg: 'bg-yellow-600/30',  text: 'text-yellow-400',  border: 'border-yellow-500' },
  Platinum:    { bg: 'bg-teal-600/30',    text: 'text-teal-400',    border: 'border-teal-500' },
  Diamond:     { bg: 'bg-blue-600/30',    text: 'text-blue-400',    border: 'border-blue-400' },
  Master:      { bg: 'bg-purple-700/30',  text: 'text-purple-400',  border: 'border-purple-500' },
  Grandmaster: { bg: 'bg-red-700/30',     text: 'text-red-400',     border: 'border-red-500' },
};

function RankBadge({ tier }: { tier: string }) {
  const config = tierConfig[tier] ?? tierConfig.Bronze;
  return (
    <span className={`inline-flex items-center px-2.5 py-0.5 rounded-full text-xs font-semibold border ${config.bg} ${config.text} ${config.border}`}>
      {tier}
    </span>
  );
}

function PositionCell({ position }: { position: number }) {
  if (position === 1) {
    return (
      <div className="flex items-center gap-2">
        <span className="text-2xl">🥇</span>
        <span className="text-lg font-bold text-yellow-400">#{position}</span>
      </div>
    );
  }
  if (position === 2) {
    return (
      <div className="flex items-center gap-2">
        <span className="text-2xl">🥈</span>
        <span className="text-lg font-bold text-gray-300">#{position}</span>
      </div>
    );
  }
  if (position === 3) {
    return (
      <div className="flex items-center gap-2">
        <span className="text-2xl">🥉</span>
        <span className="text-lg font-bold text-amber-500">#{position}</span>
      </div>
    );
  }
  return <span className="font-semibold text-gray-400 pl-2">#{position}</span>;
}

function getPositionRowClass(position: number) {
  if (position === 1) return 'bg-gradient-to-r from-yellow-500/10 to-transparent';
  if (position === 2) return 'bg-gradient-to-r from-gray-400/10 to-transparent';
  if (position === 3) return 'bg-gradient-to-r from-amber-600/10 to-transparent';
  return '';
}

function PlayerRow({ entry }: { entry: LeaderboardEntry }) {
  return (
    <motion.tr
      variants={staggerItem}
      className={`border-b border-gray-700/50 transition-colors hover:bg-white/3 ${getPositionRowClass(entry.position)}`}
    >
      <td className="py-3 px-4 w-20">
        <PositionCell position={entry.position} />
      </td>
      <td className="py-3 px-4">
        <Link to={`/profile/${entry.userId}`} className="flex items-center gap-3 group">
          <div className="w-10 h-10 rounded-full bg-gray-700 border-2 border-gray-600 overflow-hidden shrink-0">
            {entry.avatarUrl ? (
              <img src={entry.avatarUrl} alt={entry.username} className="w-full h-full object-cover" />
            ) : (
              <div className="w-full h-full flex items-center justify-center text-sm font-bold text-gray-400">
                {entry.username.slice(0, 2).toUpperCase()}
              </div>
            )}
          </div>
          <span className="font-medium text-gray-200 group-hover:text-white transition-colors">
            {entry.username}
          </span>
        </Link>
      </td>
      <td className="py-3 px-4 text-right">
        <span className="font-mono font-bold text-white text-lg">
          {entry.eloPoints.toLocaleString()}
        </span>
      </td>
      <td className="py-3 px-4 text-right">
        <RankBadge tier={entry.rankTier} />
      </td>
    </motion.tr>
  );
}

export default function LeaderboardPage() {
  const [selectedGameId, setSelectedGameId] = useState('');
  const [page, setPage] = useState(1);
  const [showRecordModal, setShowRecordModal] = useState(false);
  const pageSize = 25;

  const isAuth = useAuthStore((s) => s.isAuthenticated());
  const { data: gamesData } = useGameBrowse({ pageSize: 100 });
  const { data, isLoading, isFetching } = useLeaderboard(selectedGameId, page, pageSize);

  const games = gamesData?.items ?? [];
  const entries = data?.items ?? [];
  const totalCount = data?.totalCount ?? 0;
  const totalPages = Math.ceil(totalCount / pageSize);

  const gameOptions = [
    { value: '', label: 'Select a game...' },
    ...games.map(g => ({ value: g.id, label: g.name })),
  ];

  return (
    <AnimatedPage>
      <div className="max-w-5xl mx-auto space-y-6">
        <div className="flex flex-col sm:flex-row justify-between items-start sm:items-center gap-4">
          <div>
            <h1 className="text-3xl font-bold text-white flex items-center gap-3">
              <span className="text-4xl">🏆</span>
              Leaderboard
            </h1>
            <p className="text-gray-400 mt-1">Top players ranked by Elo rating</p>
          </div>

          <div className="flex items-center gap-3">
            {isAuth && (
              <Button onClick={() => setShowRecordModal(true)} size="sm">
                Record Match
              </Button>
            )}
            <div className="w-full sm:w-64">
              <Select
                label=""
                options={gameOptions}
                value={selectedGameId}
                onChange={e => { setSelectedGameId(e.target.value); setPage(1); }}
              />
            </div>
          </div>
        </div>

        {!selectedGameId ? (
          <div className="text-center py-20">
            <div className="text-6xl mb-4">🎮</div>
            <h2 className="text-xl font-semibold text-gray-300 mb-2">Select a Game</h2>
            <p className="text-gray-500">Choose a game from the dropdown to see its leaderboard</p>
          </div>
        ) : isLoading ? (
          <div className="flex items-center justify-center py-20">
            <Spinner size="lg" />
          </div>
        ) : entries.length === 0 ? (
          <div className="text-center py-20">
            <div className="text-6xl mb-4">📊</div>
            <h2 className="text-xl font-semibold text-gray-300 mb-2">No Rankings Yet</h2>
            <p className="text-gray-500">No players have been ranked for this game yet</p>
          </div>
        ) : (
          <>
            <div className="rounded-xl border border-gray-700/50 overflow-hidden bg-gray-800/30 backdrop-blur-sm">
              <div className="overflow-x-auto">
                <table className="w-full text-sm">
                  <thead>
                    <tr className="bg-gray-800/60 border-b border-gray-700/50">
                      <th className="py-3 px-4 text-left text-xs font-semibold text-gray-400 uppercase tracking-wider w-20">Rank</th>
                      <th className="py-3 px-4 text-left text-xs font-semibold text-gray-400 uppercase tracking-wider">Player</th>
                      <th className="py-3 px-4 text-right text-xs font-semibold text-gray-400 uppercase tracking-wider">Elo</th>
                      <th className="py-3 px-4 text-right text-xs font-semibold text-gray-400 uppercase tracking-wider">Tier</th>
                    </tr>
                  </thead>
                  <motion.tbody variants={staggerContainer} initial="hidden" animate="show">
                    {entries.map(entry => (
                      <PlayerRow key={entry.userId} entry={entry} />
                    ))}
                  </motion.tbody>
                </table>
              </div>

              {isFetching && !isLoading && (
                <div className="flex justify-center py-2">
                  <Spinner size="sm" />
                </div>
              )}
            </div>

            {totalPages > 1 && (
              <div className="flex items-center justify-between">
                <p className="text-sm text-gray-400">
                  Showing {(page - 1) * pageSize + 1}–{Math.min(page * pageSize, totalCount)} of {totalCount}
                </p>
                <div className="flex items-center gap-1">
                  <button
                    onClick={() => setPage(p => Math.max(1, p - 1))}
                    disabled={page === 1}
                    className="px-3 py-1.5 rounded-lg text-sm font-medium border border-gray-700 text-gray-300 hover:bg-gray-700/50 disabled:opacity-40 disabled:cursor-not-allowed transition-colors"
                  >
                    Previous
                  </button>
                  {Array.from({ length: Math.min(5, totalPages) }, (_, i) => {
                    let pageNum: number;
                    if (totalPages <= 5) pageNum = i + 1;
                    else if (page <= 3) pageNum = i + 1;
                    else if (page >= totalPages - 2) pageNum = totalPages - 4 + i;
                    else pageNum = page - 2 + i;

                    return (
                      <button
                        key={pageNum}
                        onClick={() => setPage(pageNum)}
                        className={`w-9 h-9 rounded-lg text-sm font-medium transition-colors ${
                          page === pageNum
                            ? 'bg-indigo-600 text-white'
                            : 'border border-gray-700 text-gray-400 hover:bg-gray-700/50'
                        }`}
                      >
                        {pageNum}
                      </button>
                    );
                  })}
                  <button
                    onClick={() => setPage(p => Math.min(totalPages, p + 1))}
                    disabled={page === totalPages}
                    className="px-3 py-1.5 rounded-lg text-sm font-medium border border-gray-700 text-gray-300 hover:bg-gray-700/50 disabled:opacity-40 disabled:cursor-not-allowed transition-colors"
                  >
                    Next
                  </button>
                </div>
              </div>
            )}
          </>
        )}
      </div>

      <RecordMatchModal
        isOpen={showRecordModal}
        onClose={() => setShowRecordModal(false)}
      />
    </AnimatedPage>
  );
}
