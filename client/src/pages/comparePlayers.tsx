import { useMemo, useState } from 'react';
import { Link } from 'react-router-dom';
import { motion } from 'framer-motion';
import { useProfile } from '@/features/profile/hooks';
import type { GameProfileResponse, ProfileDetailResponse } from '@/features/profile/types';
import { useDiscoverPlayers } from '@/features/users/hooks';
import { useEloHistory } from '@/features/elo/hooks';
import { useCompareSearchParams } from '@/features/compare/hooks';
import { buildCompareViewModel } from '@/features/compare/utils';
import type { DiscoverPlayerResponse } from '@/features/users/types';
import type { SharedGameCompareRow } from '@/features/compare/types';
import { useDebounce } from '@/hooks/useDebounce';
import {
  AnimatedPage,
  Button,
  Input,
  Spinner,
  staggerContainer,
  staggerItem,
} from '@/components/ui';
import { useToast } from '@/components/ui/toast';
import { UserAvatar } from '@/components/common/userAvatar';
import { RankBadge } from '@/components/elo/rankBadge';
import { EloChart } from '@/components/elo/eloChart';

const listVariants = staggerContainer;
const itemVariants = staggerItem;

export default function ComparePlayersPage() {
  const { addToast } = useToast();
  const { leftId, rightId, setSlot, swap, setPair } = useCompareSearchParams();
  const [leftSearch, setLeftSearch] = useState('');
  const [rightSearch, setRightSearch] = useState('');
  const debouncedLeft = useDebounce(leftSearch, 280);
  const debouncedRight = useDebounce(rightSearch, 280);

  const leftDiscover = useDiscoverPlayers(
    { search: debouncedLeft.length >= 2 ? debouncedLeft : undefined, page: 1, pageSize: 10 },
    { enabled: debouncedLeft.length >= 2 },
  );
  const rightDiscover = useDiscoverPlayers(
    { search: debouncedRight.length >= 2 ? debouncedRight : undefined, page: 1, pageSize: 10 },
    { enabled: debouncedRight.length >= 2 },
  );

  const leftProfile = useProfile(leftId);
  const rightProfile = useProfile(rightId);

  const vm = useMemo(() => {
    if (!leftProfile.data || !rightProfile.data) return null;
    return buildCompareViewModel(leftProfile.data, rightProfile.data);
  }, [leftProfile.data, rightProfile.data]);

  const spotlightGameId = vm?.sharedRows[0]?.gameId;
  const eloEnabled = Boolean(leftId && rightId && spotlightGameId);
  const leftElo = useEloHistory(leftId ?? '', spotlightGameId ?? '', { enabled: eloEnabled });
  const rightElo = useEloHistory(rightId ?? '', spotlightGameId ?? '', { enabled: eloEnabled });

  const bothReady = Boolean(leftId && rightId);
  const loadingProfiles =
    bothReady && (leftProfile.isLoading || rightProfile.isLoading || leftProfile.isFetching || rightProfile.isFetching);
  const profileError = leftProfile.isError || rightProfile.isError;

  const copyShareLink = async () => {
    if (!leftId || !rightId) {
      addToast({ type: 'error', title: 'Compare', message: 'Select two players first.' });
      return;
    }
    const url = `${window.location.origin}/compare?left=${encodeURIComponent(leftId)}&right=${encodeURIComponent(rightId)}`;
    try {
      await navigator.clipboard.writeText(url);
      addToast({ type: 'success', title: 'Copied', message: 'Share link copied to clipboard.' });
    } catch {
      addToast({ type: 'error', title: 'Copy failed', message: url });
    }
  };

  const pickPlayer = (slot: 'left' | 'right', p: DiscoverPlayerResponse) => {
    const other = slot === 'left' ? rightId : leftId;
    if (other === p.id) {
      addToast({ type: 'error', title: 'Compare', message: 'Pick a different player for each side.' });
      return;
    }
    setSlot(slot, p.id);
    if (slot === 'left') setLeftSearch('');
    else setRightSearch('');
  };

  const clearSlot = (slot: 'left' | 'right') => {
    setSlot(slot, undefined);
  };

  return (
    <AnimatedPage>
      <div className="relative mx-auto max-w-6xl space-y-8 pb-10">
        <div
          className="pointer-events-none absolute -left-6 top-0 h-48 w-48 rounded-full bg-primary/12 blur-3xl"
          aria-hidden
        />
        <div
          className="pointer-events-none absolute right-0 top-24 h-56 w-56 rounded-full bg-accent/10 blur-3xl"
          aria-hidden
        />

        <motion.header
          initial={{ opacity: 0, y: -12 }}
          animate={{ opacity: 1, y: 0 }}
          className="relative border-b border-border/60 pb-6"
        >
          <div className="flex flex-col gap-4 lg:flex-row lg:items-end lg:justify-between">
            <div>
              <p className="text-[11px] font-semibold uppercase tracking-[0.22em] text-primary/90">
                Head-to-head
              </p>
              <h1 className="mt-1 font-serif text-3xl font-bold tracking-tight text-foreground sm:text-4xl">
                Oyuncu karşılaştırma
              </h1>
              <p className="mt-2 max-w-xl text-sm leading-relaxed text-foreground-muted">
                Elo, oyun profilleri ve oda istatistiklerini yan yana gör. Bağlantıyı kopyalayarak arkadaşlarınla
                paylaş.
              </p>
            </div>
            <div className="flex flex-wrap gap-2">
              <Button variant="outline" size="sm" type="button" onClick={() => void copyShareLink()} disabled={!bothReady}>
                Linki kopyala
              </Button>
              <Button variant="ghost" size="sm" type="button" onClick={swap} disabled={!bothReady}>
                Sol / sağ değiştir
              </Button>
              <Button variant="ghost" size="sm" type="button" onClick={() => setPair(undefined, undefined)}>
                Sıfırla
              </Button>
            </div>
          </div>
        </motion.header>

        <motion.section
          variants={listVariants}
          initial="hidden"
          animate="show"
          className="relative grid gap-6 lg:grid-cols-2"
        >
          <motion.div variants={itemVariants} className="rounded-2xl border border-border/70 bg-surface/90 p-4 shadow-sm">
            <PlayerPickerColumn
              title="Sol oyuncu"
              search={leftSearch}
              onSearchChange={setLeftSearch}
              selectedId={leftId}
              discover={leftDiscover}
              onPick={(p) => pickPlayer('left', p)}
              onClear={() => clearSlot('left')}
            />
          </motion.div>
          <motion.div variants={itemVariants} className="rounded-2xl border border-border/70 bg-surface/90 p-4 shadow-sm">
            <PlayerPickerColumn
              title="Sağ oyuncu"
              search={rightSearch}
              onSearchChange={setRightSearch}
              selectedId={rightId}
              discover={rightDiscover}
              onPick={(p) => pickPlayer('right', p)}
              onClear={() => clearSlot('right')}
            />
          </motion.div>
        </motion.section>

        {!bothReady && (
          <motion.div
            initial={{ opacity: 0 }}
            animate={{ opacity: 1 }}
            className="rounded-xl border border-dashed border-border bg-surface/50 px-4 py-8 text-center text-sm text-foreground-muted"
          >
            İki oyuncu seç veya URL ile aç:{' '}
            <code className="rounded bg-surface-hover px-1.5 py-0.5 text-xs text-foreground-subtle">
              /compare?left=...&amp;right=...
            </code>
          </motion.div>
        )}

        {bothReady && loadingProfiles && (
          <div className="flex justify-center py-16">
            <Spinner size="lg" />
          </div>
        )}

        {bothReady && profileError && (
          <div className="rounded-xl border border-danger/30 bg-danger/5 px-4 py-6 text-center text-sm text-danger">
            Profiller yüklenemedi. Oyuncu ID’lerini kontrol et veya tekrar dene.
          </div>
        )}

        {bothReady && !loadingProfiles && vm && (
          <CompareBody
            vm={vm}
            leftEloHistory={leftElo.data}
            rightEloHistory={rightElo.data}
            spotlightGameName={vm.sharedRows[0]?.gameName}
            isEloLoading={leftElo.isLoading || rightElo.isLoading}
          />
        )}
      </div>
    </AnimatedPage>
  );
}

function PlayerPickerColumn({
  title,
  search,
  onSearchChange,
  selectedId,
  discover,
  onPick,
  onClear,
}: {
  title: string;
  search: string;
  onSearchChange: (v: string) => void;
  selectedId: string | undefined;
  discover: ReturnType<typeof useDiscoverPlayers>;
  onPick: (p: DiscoverPlayerResponse) => void;
  onClear: () => void;
}) {
  const items = discover.data?.items ?? [];

  return (
    <div className="space-y-3">
      <div className="flex items-center justify-between gap-2">
        <h2 className="text-xs font-bold uppercase tracking-wider text-foreground-subtle">{title}</h2>
        {selectedId && (
          <button type="button" onClick={onClear} className="text-xs font-medium text-primary hover:underline">
            Temizle
          </button>
        )}
      </div>
      <Input
        value={search}
        onChange={(e) => onSearchChange(e.target.value)}
        placeholder="Kullanıcı adı ara (min 2 harf)…"
        aria-label={`${title} search`}
      />
      {search.length > 0 && search.length < 2 && (
        <p className="text-xs text-foreground-muted">En az 2 karakter yaz.</p>
      )}
      {discover.isFetching && <Spinner size="sm" />}
      {items.length > 0 && (
        <ul className="max-h-56 space-y-1 overflow-y-auto rounded-lg border border-border/60 bg-background/40 p-1">
          {items.map((p) => (
            <li key={p.id}>
              <button
                type="button"
                disabled={p.isBlockedByMe}
                onClick={() => onPick(p)}
                className="flex w-full items-center gap-2 rounded-md px-2 py-2 text-left text-sm transition-colors hover:bg-surface-hover disabled:cursor-not-allowed disabled:opacity-40"
              >
                <UserAvatar username={p.username} avatarUrl={p.avatarUrl} size="sm" />
                <span className="min-w-0 flex-1 truncate font-medium text-foreground">{p.username}</span>
                {p.isBlockedByMe && <span className="text-[10px] text-danger">Engelli</span>}
              </button>
            </li>
          ))}
        </ul>
      )}
      {selectedId && (
        <p className="text-xs text-foreground-muted">
          Seçili ID: <span className="font-mono text-foreground-subtle">{selectedId}</span>
        </p>
      )}
    </div>
  );
}

function CompareBody({
  vm,
  leftEloHistory,
  rightEloHistory,
  spotlightGameName,
  isEloLoading,
}: {
  vm: ReturnType<typeof buildCompareViewModel>;
  leftEloHistory: { history: { points: number; recordedAt: string }[] } | undefined;
  rightEloHistory: { history: { points: number; recordedAt: string }[] } | undefined;
  spotlightGameName: string | undefined;
  isEloLoading: boolean;
}) {
  const { left, right, headline, sharedRows, onlyLeftGames, onlyRightGames } = vm;

  return (
    <motion.div variants={listVariants} initial="hidden" animate="show" className="space-y-8">
      <motion.div
        variants={itemVariants}
        className="relative overflow-hidden rounded-2xl border border-border bg-linear-to-br from-surface via-surface to-primary/5 p-6"
      >
        <div className="grid gap-8 lg:grid-cols-2">
          <PlayerHero profile={left} side="left" />
          <PlayerHero profile={right} side="right" />
        </div>
        <div className="mt-6 grid gap-3 border-t border-border/60 pt-6 sm:grid-cols-3">
          <StatChip label="Ortak oyun" value={String(headline.sharedGameCount)} hint="Her iki profilde de" />
          <StatChip
            label="Ort. Elo (sol)"
            value={headline.avgEloLeft != null ? String(headline.avgEloLeft) : '—'}
            hint={`${headline.totalGamesLeft} oyun profili`}
          />
          <StatChip
            label="Ort. Elo (sağ)"
            value={headline.avgEloRight != null ? String(headline.avgEloRight) : '—'}
            hint={`${headline.totalGamesRight} oyun profili`}
          />
        </div>
      </motion.div>

      {sharedRows.length > 0 && spotlightGameName && (
        <motion.section variants={itemVariants} className="space-y-3">
          <div className="flex items-end justify-between gap-2">
            <h2 className="text-lg font-bold text-foreground">Elo geçmişi</h2>
            <span className="text-xs text-foreground-muted">{spotlightGameName} (en yüksek Elo’lu ortak oyun)</span>
          </div>
          {isEloLoading ? (
            <div className="flex justify-center py-8">
              <Spinner />
            </div>
          ) : (
            <div className="grid gap-4 lg:grid-cols-2">
              <div className="rounded-xl border border-border/60 bg-surface/80 p-3">
                <p className="mb-2 text-xs font-semibold text-foreground-subtle">{left.displayName ?? left.username}</p>
                {leftEloHistory?.history?.length ? (
                  <EloChart history={leftEloHistory.history} tierColor="#22c55e" height={160} />
                ) : (
                  <p className="py-8 text-center text-xs text-foreground-muted">Bu oyun için geçmiş yok.</p>
                )}
              </div>
              <div className="rounded-xl border border-border/60 bg-surface/80 p-3">
                <p className="mb-2 text-xs font-semibold text-foreground-subtle">{right.displayName ?? right.username}</p>
                {rightEloHistory?.history?.length ? (
                  <EloChart history={rightEloHistory.history} tierColor="#38bdf8" height={160} />
                ) : (
                  <p className="py-8 text-center text-xs text-foreground-muted">Bu oyun için geçmiş yok.</p>
                )}
              </div>
            </div>
          )}
        </motion.section>
      )}

      <motion.section variants={itemVariants} className="space-y-3">
        <h2 className="text-lg font-bold text-foreground">Ortak oyun profilleri</h2>
        {sharedRows.length === 0 ? (
          <p className="rounded-xl border border-border/60 bg-surface/60 px-4 py-6 text-sm text-foreground-muted">
            Ortak oyun profili yok. İki oyuncunun da aynı oyun için profil eklemesi gerekir.
          </p>
        ) : (
          <div className="overflow-x-auto rounded-xl border border-border/60">
            <table className="w-full min-w-[640px] text-left text-sm">
              <thead className="border-b border-border bg-surface-hover/80 text-xs uppercase tracking-wide text-foreground-subtle">
                <tr>
                  <th className="px-3 py-2">Oyun</th>
                  <th className="px-3 py-2">Sol Elo</th>
                  <th className="px-3 py-2">Sağ Elo</th>
                  <th className="px-3 py-2">Fark</th>
                  <th className="px-3 py-2">Roller</th>
                </tr>
              </thead>
              <tbody>
                {sharedRows.map((row) => (
                  <SharedGameRow key={row.gameId} row={row} />
                ))}
              </tbody>
            </table>
          </div>
        )}
      </motion.section>

      {(onlyLeftGames.length > 0 || onlyRightGames.length > 0) && (
        <motion.section variants={itemVariants} className="grid gap-6 lg:grid-cols-2">
          <UniqueGames title={`Sadece ${left.displayName ?? left.username}`} games={onlyLeftGames} />
          <UniqueGames title={`Sadece ${right.displayName ?? right.username}`} games={onlyRightGames} />
        </motion.section>
      )}
    </motion.div>
  );
}

function PlayerHero({ profile, side }: { profile: ProfileDetailResponse; side: 'left' | 'right' }) {
  const align = side === 'left' ? 'items-start text-left' : 'items-end text-right lg:items-start lg:text-left';
  return (
    <div className={`flex flex-col gap-3 ${align}`}>
      <div className="flex flex-col gap-3 sm:flex-row sm:items-center sm:gap-4">
        <UserAvatar
          username={profile.username}
          avatarUrl={profile.avatarUrl}
          size="lg"
          className={side === 'right' ? 'sm:order-2' : ''}
        />
        <div>
          <Link to={`/profile/${profile.userId}`} className="text-xl font-bold text-foreground hover:text-primary">
            {profile.displayName ?? profile.username}
          </Link>
          <p className="text-xs text-foreground-muted">@{profile.username}</p>
          {profile.country && (
            <p className="mt-1 text-xs text-foreground-subtle">{profile.country}</p>
          )}
        </div>
      </div>
      <div className="flex flex-wrap gap-2">
        <RankBadge tier={profile.games[0]?.rankTier ?? 'Gold'} eloPoints={profile.games[0]?.eloPoints} size="sm" />
        <span className="rounded-full border border-border/80 bg-background/50 px-2 py-0.5 text-xs text-foreground-muted">
          Odalar: {profile.stats.roomsJoined} katılım · {profile.stats.roomsCreated} oluşturma
        </span>
      </div>
    </div>
  );
}

function StatChip({ label, value, hint }: { label: string; value: string; hint: string }) {
  return (
    <div className="rounded-lg border border-border/50 bg-background/30 px-3 py-2">
      <p className="text-[10px] font-semibold uppercase tracking-wide text-foreground-subtle">{label}</p>
      <p className="text-lg font-bold text-foreground">{value}</p>
      <p className="text-[11px] text-foreground-muted">{hint}</p>
    </div>
  );
}

function SharedGameRow({ row }: { row: SharedGameCompareRow }) {
  const diffLabel = row.eloDiff === 0 ? '0' : row.eloDiff > 0 ? `+${row.eloDiff}` : `${row.eloDiff}`;
  const diffTone =
    row.eloDiff === 0 ? 'text-foreground-muted' : row.eloDiff > 0 ? 'text-success' : 'text-accent';

  return (
    <tr className="border-b border-border/40 last:border-0 hover:bg-surface-hover/40">
      <td className="px-3 py-3">
        <div className="flex items-center gap-2">
          {row.gameImageUrl ? (
            <img src={row.gameImageUrl} alt="" className="h-10 w-14 rounded-md object-cover" />
          ) : (
            <div className="flex h-10 w-14 items-center justify-center rounded-md bg-surface-hover text-xs">🎮</div>
          )}
          <span className="font-medium text-foreground">{row.gameName}</span>
        </div>
      </td>
      <td className="px-3 py-3">
        <div className="flex flex-col gap-1">
          <span className="font-semibold text-foreground">{row.left.eloPoints}</span>
          <RankBadge tier={row.left.rankTier} eloPoints={row.left.eloPoints} size="sm" />
        </div>
      </td>
      <td className="px-3 py-3">
        <div className="flex flex-col gap-1">
          <span className="font-semibold text-foreground">{row.right.eloPoints}</span>
          <RankBadge tier={row.right.rankTier} eloPoints={row.right.eloPoints} size="sm" />
        </div>
      </td>
      <td className={`px-3 py-3 font-mono text-sm font-bold ${diffTone}`}>{diffLabel}</td>
      <td className="px-3 py-3 text-xs text-foreground-muted">
        <span className="block">L: {row.left.role ?? '—'} · {row.left.region}</span>
        <span className="block">R: {row.right.role ?? '—'} · {row.right.region}</span>
      </td>
    </tr>
  );
}

function UniqueGames({ title, games }: { title: string; games: GameProfileResponse[] }) {
  if (games.length === 0) return null;
  return (
    <div className="rounded-xl border border-border/60 bg-surface/70 p-4">
      <h3 className="text-sm font-bold text-foreground">{title}</h3>
      <ul className="mt-3 space-y-2 text-sm">
        {games.slice(0, 8).map((g) => (
          <li key={g.id} className="flex items-center justify-between gap-2 rounded-lg bg-background/30 px-2 py-1.5">
            <span className="truncate font-medium text-foreground">{g.gameName}</span>
            <span className="shrink-0 text-xs text-foreground-muted">{g.eloPoints} Elo</span>
          </li>
        ))}
      </ul>
      {games.length > 8 && <p className="mt-2 text-xs text-foreground-muted">+{games.length - 8} daha…</p>}
    </div>
  );
}
