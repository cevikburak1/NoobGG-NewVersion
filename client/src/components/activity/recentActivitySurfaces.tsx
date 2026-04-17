import { Link } from 'react-router-dom';
import { motion } from 'framer-motion';
import { useRecentActivity } from '@/features/activity/hooks';
import type { RecentConversationItem, RecentPlayerItem, RecentRoomItem } from '@/features/activity/types';
import { EmptyState } from '@/components/common/emptyState';
import { UserAvatar } from '@/components/common/userAvatar';
import { resolveFileUrl } from '@/lib/api';
import { Button, Spinner } from '@/components/ui';

const secondaryLinkClass =
  'inline-flex items-center justify-center rounded-lg border border-border bg-surface px-3 py-1.5 text-sm font-medium text-foreground transition-colors hover:bg-surface-hover focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-primary/50';

function formatRelative(iso: string): string {
  const t = new Date(iso).getTime();
  if (Number.isNaN(t)) return '';
  const diffMs = Date.now() - t;
  const mins = Math.floor(diffMs / 60_000);
  if (mins < 1) return 'Just now';
  if (mins < 60) return `${mins}m ago`;
  const hrs = Math.floor(mins / 60);
  if (hrs < 24) return `${hrs}h ago`;
  const days = Math.floor(hrs / 24);
  if (days < 7) return `${days}d ago`;
  return new Date(iso).toLocaleDateString();
}

function sourcePill(source: RecentPlayerItem['source']): { label: string; className: string } {
  switch (source) {
    case 'directMessage':
      return { label: 'DM', className: 'bg-sky-500/15 text-sky-300 ring-1 ring-sky-500/25' };
    case 'friendship':
      return { label: 'Friend', className: 'bg-emerald-500/15 text-emerald-300 ring-1 ring-emerald-500/25' };
    case 'room':
    default:
      return { label: 'Room', className: 'bg-violet-500/15 text-violet-200 ring-1 ring-violet-500/25' };
  }
}

const hubCardMotion = {
  initial: { opacity: 0, y: 14 },
  animate: { opacity: 1, y: 0 },
};

export function RecentActivityHub() {
  const { data, isLoading, isError, refetch, isFetching } = useRecentActivity();

  if (isLoading) {
    return (
      <div className="grid gap-4 lg:grid-cols-3">
        {[0, 1, 2].map((i) => (
          <div
            key={i}
            className="h-48 animate-pulse rounded-2xl border border-border/60 bg-surface-hover/40"
            aria-hidden
          />
        ))}
      </div>
    );
  }

  if (isError || !data) {
    return (
      <div className="rounded-2xl border border-danger/25 bg-danger/5 px-5 py-4">
        <p className="text-sm text-danger">Could not load recent activity.</p>
        <Button variant="ghost" size="sm" className="mt-2" onClick={() => void refetch()}>
          Retry
        </Button>
      </div>
    );
  }

  return (
    <section
      className="relative overflow-hidden rounded-2xl border border-primary/15 bg-linear-to-br from-primary/[0.07] via-surface/90 to-accent/6 p-5 shadow-[0_0_0_1px_rgba(255,255,255,0.04)_inset]"
      aria-labelledby="recent-activity-heading"
    >
      <div
        className="pointer-events-none absolute -right-16 -top-20 h-56 w-56 rounded-full bg-primary/10 blur-3xl"
        aria-hidden
      />
      <div className="relative flex flex-col gap-1 sm:flex-row sm:items-end sm:justify-between">
        <div>
          <h2 id="recent-activity-heading" className="text-lg font-bold tracking-tight text-foreground">
            Pick up where you left off
          </h2>
          <p className="mt-0.5 max-w-xl text-sm text-foreground-muted">
            People and rooms you have actually touched — fast paths back in without hunting the feed.
          </p>
        </div>
        {isFetching && !isLoading ? (
          <span className="text-xs text-foreground-subtle">Refreshing…</span>
        ) : null}
      </div>

      <div className="relative mt-5 grid gap-4 lg:grid-cols-3">
        <motion.div {...hubCardMotion} transition={{ delay: 0.05 }} className="flex flex-col rounded-xl border border-border/70 bg-surface/85 p-4 backdrop-blur-sm">
          <div className="flex items-center justify-between gap-2">
            <h3 className="text-sm font-semibold text-foreground">Recent players</h3>
            <Link to="/discover" className="text-xs font-medium text-primary hover:underline">
              Discover
            </Link>
          </div>
          <div className="mt-3 flex-1 space-y-2">
            {data.recentPlayers.length === 0 ? (
              <EmptyState
                className="py-8"
                title="No one yet"
                description="Join a room or send a DM — your squad will appear here."
                action={<Link to="/rooms" className={secondaryLinkClass}>Browse rooms</Link>}
              />
            ) : (
              data.recentPlayers.map((p) => <PlayerRow key={p.userId} player={p} />)
            )}
          </div>
        </motion.div>

        <motion.div {...hubCardMotion} transition={{ delay: 0.1 }} className="flex flex-col rounded-xl border border-border/70 bg-surface/85 p-4 backdrop-blur-sm">
          <div className="flex items-center justify-between gap-2">
            <h3 className="text-sm font-semibold text-foreground">Recently messaged</h3>
            <Link to="/messages" className="text-xs font-medium text-primary hover:underline">
              Inbox
            </Link>
          </div>
          <div className="mt-3 flex-1 space-y-2">
            {data.recentConversations.length === 0 ? (
              <EmptyState
                className="py-8"
                title="No threads"
                description="Open a profile and tap Send Message to start a conversation."
                action={<Link to="/discover" className={secondaryLinkClass}>Find players</Link>}
              />
            ) : (
              data.recentConversations.map((c) => <ConversationRow key={c.id} conversation={c} />)
            )}
          </div>
        </motion.div>

        <motion.div {...hubCardMotion} transition={{ delay: 0.15 }} className="flex flex-col rounded-xl border border-border/70 bg-surface/85 p-4 backdrop-blur-sm">
          <div className="flex items-center justify-between gap-2">
            <h3 className="text-sm font-semibold text-foreground">Recently joined rooms</h3>
            <Link to="/rooms" className="text-xs font-medium text-primary hover:underline">
              All rooms
            </Link>
          </div>
          <div className="mt-3 flex-1 space-y-2">
            {data.recentRooms.length === 0 ? (
              <EmptyState
                className="py-8"
                title="No rooms yet"
                description="Create or join a room — your latest memberships land here."
                action={<Link to="/rooms" className={secondaryLinkClass}>Open rooms</Link>}
              />
            ) : (
              data.recentRooms.map((r) => <RoomRow key={r.roomId} room={r} />)
            )}
          </div>
        </motion.div>
      </div>
    </section>
  );
}

function PlayerRow({ player }: { player: RecentPlayerItem }) {
  const pill = sourcePill(player.source);
  return (
    <Link
      to={`/profile/${player.userId}`}
      className="group flex items-center gap-3 rounded-lg border border-transparent px-2 py-2 transition-colors hover:border-border hover:bg-surface-hover/80"
    >
      <UserAvatar username={player.username} avatarUrl={player.avatarUrl} size="sm" />
      <div className="min-w-0 flex-1">
        <p className="truncate text-sm font-medium text-foreground group-hover:text-primary">{player.username}</p>
        <p className="text-[11px] text-foreground-subtle">{formatRelative(player.lastInteractionAt)}</p>
      </div>
      <span className={`shrink-0 rounded-full px-2 py-0.5 text-[10px] font-semibold uppercase tracking-wide ${pill.className}`}>
        {pill.label}
      </span>
    </Link>
  );
}

function ConversationRow({ conversation }: { conversation: RecentConversationItem }) {
  return (
    <Link
      to={`/messages?user=${conversation.partnerId}`}
      className="group flex items-center gap-3 rounded-lg border border-transparent px-2 py-2 transition-colors hover:border-border hover:bg-surface-hover/80"
    >
      <UserAvatar
        username={conversation.partnerUsername}
        avatarUrl={conversation.partnerAvatarUrl}
        size="sm"
      />
      <div className="min-w-0 flex-1">
        <p className="truncate text-sm font-medium text-foreground group-hover:text-primary">
          {conversation.partnerUsername}
        </p>
        <p className="truncate text-[11px] text-foreground-muted">
          {conversation.lastMessageContent?.trim() || 'Open thread'}
        </p>
      </div>
      <div className="shrink-0 text-right">
        {conversation.lastMessageAt ? (
          <p className="text-[10px] text-foreground-subtle">{formatRelative(conversation.lastMessageAt)}</p>
        ) : null}
        {conversation.unreadCount > 0 ? (
          <span className="mt-0.5 inline-flex min-w-[18px] justify-center rounded-full bg-primary px-1 text-[10px] font-bold text-primary-foreground">
            {conversation.unreadCount > 9 ? '9+' : conversation.unreadCount}
          </span>
        ) : null}
      </div>
    </Link>
  );
}

function RoomRow({ room }: { room: RecentRoomItem }) {
  return (
    <Link
      to={`/rooms/${room.roomId}`}
      className="group flex items-center gap-3 rounded-lg border border-transparent px-2 py-2 transition-colors hover:border-border hover:bg-surface-hover/80"
    >
      <div className="h-10 w-10 shrink-0 overflow-hidden rounded-lg border border-border/60 bg-surface-hover">
        {room.gameImageUrl ? (
          <img src={resolveFileUrl(room.gameImageUrl) ?? ''} alt="" className="h-full w-full object-cover" />
        ) : (
          <div className="flex h-full w-full items-center justify-center text-xs text-foreground-subtle">🎮</div>
        )}
      </div>
      <div className="min-w-0 flex-1">
        <p className="truncate text-sm font-medium text-foreground group-hover:text-primary">{room.title}</p>
        <p className="truncate text-[11px] text-foreground-muted">
          {room.gameName ?? 'Game'} · {room.region} · {formatRelative(room.joinedAt)}
        </p>
      </div>
      <span className="shrink-0 text-[10px] text-foreground-subtle">{room.currentMemberCount} in</span>
    </Link>
  );
}

export function RecentJoinedRoomsMini() {
  const { data, isLoading } = useRecentActivity();

  if (isLoading) {
    return (
      <div className="flex items-center gap-2 rounded-xl border border-border/60 bg-surface/60 px-3 py-2">
        <Spinner size="sm" />
        <span className="text-xs text-foreground-muted">Loading recent rooms…</span>
      </div>
    );
  }

  const rooms = data?.recentRooms ?? [];
  if (rooms.length === 0) {
    return (
      <div className="rounded-xl border border-dashed border-border/80 bg-surface/40 px-4 py-3">
        <p className="text-xs font-medium text-foreground">Recent rooms</p>
        <p className="mt-0.5 text-[11px] text-foreground-muted">Join a room to see quick re-entry chips here.</p>
        <Link to="/rooms" className="mt-2 inline-block text-xs font-semibold text-primary hover:underline">
          Browse rooms
        </Link>
      </div>
    );
  }

  return (
    <div className="rounded-xl border border-border/60 bg-surface/60 p-3">
      <div className="flex items-center justify-between gap-2">
        <p className="text-xs font-semibold uppercase tracking-wide text-foreground-muted">Recent rooms</p>
        <Link to="/rooms" className="text-[11px] font-medium text-primary hover:underline">
          See all
        </Link>
      </div>
      <div className="mt-2 flex gap-2 overflow-x-auto pb-1 [-ms-overflow-style:none] [scrollbar-width:none] [&::-webkit-scrollbar]:hidden">
        {rooms.slice(0, 8).map((r) => (
          <Link
            key={r.roomId}
            to={`/rooms/${r.roomId}`}
            className="flex min-w-[140px] max-w-[180px] shrink-0 flex-col rounded-lg border border-border/70 bg-surface/90 px-2.5 py-2 transition-colors hover:border-primary/40"
          >
            <span className="truncate text-xs font-semibold text-foreground">{r.title}</span>
            <span className="truncate text-[10px] text-foreground-subtle">{formatRelative(r.joinedAt)}</span>
          </Link>
        ))}
      </div>
    </div>
  );
}

export function RecentMessagedMini() {
  const { data, isLoading } = useRecentActivity();

  if (isLoading) {
    return (
      <div className="flex items-center justify-center gap-2 border-b border-border py-2">
        <Spinner size="sm" />
      </div>
    );
  }

  const convs = data?.recentConversations ?? [];
  if (convs.length === 0) {
    return (
      <div className="border-b border-border px-3 py-2.5">
        <p className="text-[11px] font-medium text-foreground">Jump back in</p>
        <p className="mt-0.5 text-[10px] text-foreground-muted">No recent threads — start from a profile.</p>
        <Link to="/discover" className="mt-1 inline-block text-[10px] font-semibold text-primary hover:underline">
          Discover players
        </Link>
      </div>
    );
  }

  return (
    <div className="border-b border-border px-2 py-2">
      <p className="mb-1.5 px-1 text-[10px] font-semibold uppercase tracking-wide text-foreground-muted">Jump back in</p>
      <div className="flex flex-wrap gap-1.5">
        {convs.slice(0, 6).map((c) => (
          <Link
            key={c.id}
            to={`/messages?user=${c.partnerId}`}
            className="flex items-center gap-1.5 rounded-full border border-border/70 bg-surface-hover/80 py-1 pl-1 pr-2.5 text-[11px] font-medium text-foreground transition-colors hover:border-primary/40 hover:text-primary"
          >
            <UserAvatar username={c.partnerUsername} avatarUrl={c.partnerAvatarUrl} size="xs" />
            <span className="max-w-[88px] truncate">{c.partnerUsername}</span>
            {c.unreadCount > 0 ? (
              <span className="ml-0.5 flex h-4 min-w-[16px] items-center justify-center rounded-full bg-primary px-1 text-[9px] font-bold text-primary-foreground">
                {c.unreadCount > 9 ? '9+' : c.unreadCount}
              </span>
            ) : null}
          </Link>
        ))}
      </div>
    </div>
  );
}
