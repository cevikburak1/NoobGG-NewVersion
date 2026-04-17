import { useState } from 'react';
import { useParams, useNavigate, Link } from 'react-router-dom';
import { motion, AnimatePresence } from 'framer-motion';
import {
  useGuildDetail,
  useJoinGuild,
  useLeaveGuild,
  useKickGuildMember,
  useUpdateGuildMemberRole,
  useInviteToGuild,
} from '@/features/guilds/hooks';
import { useAuthStore } from '@/stores/authStore';
import { useDebounce } from '@/hooks/useDebounce';
import { discoverPlayers } from '@/features/users/api';
import type { GuildMemberResponse, GuildGameInfo } from '@/features/guilds/types';
import {
  Button, Badge, Modal, Input, AnimatedPage, Spinner, staggerContainer, staggerItem,
} from '@/components/ui';
import { UserAvatar } from '@/components/common/userAvatar';

export default function GuildDetailPage() {
  const { guildId } = useParams<{ guildId: string }>();
  const navigate = useNavigate();
  const userId = useAuthStore((s) => s.user?.id);
  const isAuth = useAuthStore((s) => s.isAuthenticated());

  const { data: guild, isLoading } = useGuildDetail(guildId);
  const joinGuild = useJoinGuild();
  const leaveGuild = useLeaveGuild();
  const kickMember = useKickGuildMember();
  const updateRole = useUpdateGuildMemberRole();

  const [showInviteModal, setShowInviteModal] = useState(false);

  if (isLoading) {
    return (
      <AnimatedPage>
        <div className="flex justify-center py-20"><Spinner size="lg" /></div>
      </AnimatedPage>
    );
  }

  if (!guild) {
    return (
      <AnimatedPage>
        <div className="flex flex-col items-center py-20 text-center">
          <span className="text-5xl">🔍</span>
          <h3 className="mt-4 text-xl font-bold text-foreground">Guild not found</h3>
          <Button className="mt-4" onClick={() => navigate('/guilds')}>Browse Guilds</Button>
        </div>
      </AnimatedPage>
    );
  }

  const isMember = guild.members.some((m) => m.userId === userId);
  const currentMember = guild.members.find((m) => m.userId === userId);
  const isOwner = currentMember?.role === 'Owner';
  const isAdmin = currentMember?.role === 'Admin';
  const canManage = isOwner || isAdmin;
  const capacityPercent = guild.maxMembers > 0 ? (guild.currentMemberCount / guild.maxMembers) * 100 : 0;

  const handleJoin = () => {
    if (!isAuth) { navigate('/login'); return; }
    joinGuild.mutate(guild.id);
  };

  const handleLeave = () => leaveGuild.mutate(guild.id, {
    onSuccess: () => navigate('/guilds'),
  });

  const handleKick = (targetUserId: string) => {
    if (!guildId) return;
    kickMember.mutate({ guildId, userId: targetUserId });
  };

  const handleRoleChange = (targetUserId: string, newRole: string) => {
    if (!guildId) return;
    updateRole.mutate({ guildId, userId: targetUserId, newRole });
  };

  return (
    <AnimatedPage>
      <div className="space-y-6">
        {/* Header */}
        <motion.div
          initial={{ opacity: 0, y: -10 }}
          animate={{ opacity: 1, y: 0 }}
          className="rounded-xl border border-border bg-surface overflow-hidden"
        >
          <div className="bg-linear-to-br from-primary/15 via-accent/10 to-primary/5 px-6 py-8">
            <div className="flex flex-col gap-4 sm:flex-row sm:items-center sm:justify-between">
              <div>
                <div className="flex items-center gap-3">
                  <span className="rounded-lg bg-primary/20 px-3 py-1 text-sm font-bold text-primary">
                    [{guild.tag}]
                  </span>
                  <h1 className="text-3xl font-bold text-foreground">{guild.name}</h1>
                </div>
                {guild.description && (
                  <p className="mt-2 max-w-2xl text-sm text-foreground-muted">{guild.description}</p>
                )}
                <div className="mt-3 flex flex-wrap gap-2">
                  <Badge>{guild.region}</Badge>
                  <Badge>{guild.language}</Badge>
                  <Badge variant={guild.isPublic ? 'success' : 'warning'}>
                    {guild.isPublic ? 'Public' : 'Private'}
                  </Badge>
                </div>
              </div>
              <div className="flex flex-col items-end gap-2">
                <div className="text-center">
                  <span className="text-3xl font-bold text-primary">{guild.currentMemberCount}</span>
                  <span className="ml-1 text-sm text-foreground-muted">/ {guild.maxMembers}</span>
                  <p className="text-xs text-foreground-subtle">members</p>
                </div>
                {!isMember ? (
                  <Button onClick={handleJoin} isLoading={joinGuild.isPending}>
                    Join Guild
                  </Button>
                ) : isOwner ? (
                  <div className="flex gap-2">
                    <Button variant="outline" size="sm" onClick={() => setShowInviteModal(true)}>
                      Invite
                    </Button>
                  </div>
                ) : (
                  <div className="flex gap-2">
                    {canManage && (
                      <Button variant="outline" size="sm" onClick={() => setShowInviteModal(true)}>
                        Invite
                      </Button>
                    )}
                    <Button variant="danger" size="sm" onClick={handleLeave} isLoading={leaveGuild.isPending}>
                      Leave
                    </Button>
                  </div>
                )}
              </div>
            </div>
          </div>

          <div className="px-6 py-2">
            <div className="h-2 w-full overflow-hidden rounded-full bg-surface-hover">
              <motion.div
                initial={{ width: 0 }}
                animate={{ width: `${Math.min(capacityPercent, 100)}%` }}
                transition={{ duration: 0.8 }}
                className={`h-full rounded-full ${
                  capacityPercent >= 90 ? 'bg-danger' : capacityPercent >= 70 ? 'bg-warning' : 'bg-accent'
                }`}
              />
            </div>
          </div>

          <div className="flex items-center gap-3 border-t border-border/50 px-6 py-3">
            <Link
              to={`/guilds/${guild.id}/stats`}
              className="inline-flex items-center gap-2 rounded-lg bg-primary/10 px-4 py-2 text-sm font-medium text-primary transition-colors hover:bg-primary/20"
            >
              <svg className="h-4 w-4" fill="none" stroke="currentColor" viewBox="0 0 24 24" strokeWidth={2}>
                <path strokeLinecap="round" strokeLinejoin="round" d="M3 13.125C3 12.504 3.504 12 4.125 12h2.25c.621 0 1.125.504 1.125 1.125v6.75C7.5 20.496 6.996 21 6.375 21h-2.25A1.125 1.125 0 013 19.875v-6.75zM9.75 8.625c0-.621.504-1.125 1.125-1.125h2.25c.621 0 1.125.504 1.125 1.125v11.25c0 .621-.504 1.125-1.125 1.125h-2.25a1.125 1.125 0 01-1.125-1.125V8.625zM16.5 4.125c0-.621.504-1.125 1.125-1.125h2.25C20.496 3 21 3.504 21 4.125v15.75c0 .621-.504 1.125-1.125 1.125h-2.25a1.125 1.125 0 01-1.125-1.125V4.125z" />
              </svg>
              Guild Stats & Events
            </Link>
          </div>
        </motion.div>

        <div className="grid gap-6 lg:grid-cols-3">
          {/* Games */}
          {guild.games.length > 0 && (
            <motion.div
              initial={{ opacity: 0, y: 10 }}
              animate={{ opacity: 1, y: 0 }}
              transition={{ delay: 0.1 }}
              className="lg:col-span-2 rounded-xl border border-border bg-surface p-5"
            >
              <h2 className="mb-4 text-lg font-semibold text-foreground">Games</h2>
              <div className="grid gap-3 sm:grid-cols-2">
                {guild.games.map((game) => (
                  <GameCard key={game.id} game={game} />
                ))}
              </div>
            </motion.div>
          )}

          {/* Members */}
          <motion.div
            initial={{ opacity: 0, y: 10 }}
            animate={{ opacity: 1, y: 0 }}
            transition={{ delay: 0.2 }}
            className={`rounded-xl border border-border bg-surface p-5 ${
              guild.games.length === 0 ? 'lg:col-span-3' : ''
            }`}
          >
            <div className="mb-4 flex items-center justify-between">
              <h2 className="text-lg font-semibold text-foreground">
                Members ({guild.currentMemberCount})
              </h2>
            </div>
            <motion.div
              variants={staggerContainer}
              initial="hidden"
              animate="show"
              className="space-y-2"
            >
              {guild.members.map((member) => (
                <MemberRow
                  key={member.userId}
                  member={member}
                  isOwner={isOwner}
                  isAdmin={isAdmin}
                  isSelf={member.userId === userId}
                  onKick={() => handleKick(member.userId)}
                  onRoleChange={(role) => handleRoleChange(member.userId, role)}
                />
              ))}
            </motion.div>
          </motion.div>
        </div>

        {(joinGuild.error || leaveGuild.error) && (
          <motion.p
            initial={{ opacity: 0 }}
            animate={{ opacity: 1 }}
            className="rounded-md bg-danger/10 px-4 py-3 text-sm text-danger"
          >
            {((joinGuild.error || leaveGuild.error) as any)?.response?.data?.title ?? 'An error occurred'}
          </motion.p>
        )}

        {canManage && guildId && (
          <InvitePlayerModal
            isOpen={showInviteModal}
            onClose={() => setShowInviteModal(false)}
            guildId={guildId}
          />
        )}
      </div>
    </AnimatedPage>
  );
}

function GameCard({ game }: { game: GuildGameInfo }) {
  return (
    <Link to={`/games/${game.id}`}>
      <motion.div
        whileHover={{ y: -2 }}
        className="group flex items-center gap-3 rounded-lg border border-border/60 p-3 transition-colors hover:border-primary/30 hover:bg-surface-hover"
      >
        {game.backgroundImageUrl ? (
          <img
            src={game.backgroundImageUrl}
            alt={game.name}
            className="h-12 w-16 shrink-0 rounded-lg object-cover"
          />
        ) : (
          <div className="flex h-12 w-16 shrink-0 items-center justify-center rounded-lg bg-surface-hover text-lg">
            🎮
          </div>
        )}
        <span className="min-w-0 flex-1 truncate font-medium text-foreground group-hover:text-primary transition-colors">
          {game.name}
        </span>
      </motion.div>
    </Link>
  );
}

const roleColors: Record<string, string> = {
  Owner: 'text-warning',
  Admin: 'text-primary',
  Member: 'text-foreground-muted',
};

const roleBadgeVariant: Record<string, 'warning' | 'primary' | 'default'> = {
  Owner: 'warning',
  Admin: 'primary',
  Member: 'default',
};

function MemberRow({
  member,
  isOwner,
  isAdmin,
  isSelf,
  onKick,
  onRoleChange,
}: {
  member: GuildMemberResponse;
  isOwner: boolean;
  isAdmin: boolean;
  isSelf: boolean;
  onKick: () => void;
  onRoleChange: (role: string) => void;
}) {
  const canKick =
    !isSelf &&
    member.role !== 'Owner' &&
    (isOwner || (isAdmin && member.role === 'Member'));

  const canChangeRole = isOwner && !isSelf && member.role !== 'Owner';

  return (
    <motion.div
      variants={staggerItem}
      className="flex items-center gap-3 rounded-lg px-3 py-2 transition-colors hover:bg-surface-hover"
    >
      <Link to={`/profile/${member.userId}`} className="shrink-0">
        <UserAvatar username={member.username} avatarUrl={member.avatarUrl} size="sm" className="h-9 w-9" />
      </Link>
      <div className="min-w-0 flex-1">
        <Link to={`/profile/${member.userId}`} className="block truncate text-sm font-medium text-foreground hover:text-primary transition-colors">
          {member.username}
        </Link>
        <Badge variant={roleBadgeVariant[member.role] ?? 'default'} className="text-[10px]">
          {member.role}
        </Badge>
      </div>
      <div className="flex shrink-0 items-center gap-1">
        {canChangeRole && (
          <select
            className="rounded border border-border bg-surface px-2 py-1 text-xs text-foreground"
            value={member.role}
            onChange={(e) => onRoleChange(e.target.value)}
          >
            <option value="Admin">Admin</option>
            <option value="Member">Member</option>
          </select>
        )}
        {canKick && (
          <button
            onClick={onKick}
            className="rounded p-1 text-foreground-muted hover:bg-danger/10 hover:text-danger transition-colors"
            title="Kick member"
          >
            <svg className="h-4 w-4" fill="none" stroke="currentColor" viewBox="0 0 24 24" strokeWidth={1.5}>
              <path strokeLinecap="round" strokeLinejoin="round" d="M22 10.5h-6m-2.25-4.125a3.375 3.375 0 11-6.75 0 3.375 3.375 0 016.75 0zM4 19.235v-.11a6.375 6.375 0 0112.75 0v.109A12.318 12.318 0 0110.374 21c-2.331 0-4.512-.645-6.374-1.766z" />
            </svg>
          </button>
        )}
      </div>
    </motion.div>
  );
}

function InvitePlayerModal({
  isOpen,
  onClose,
  guildId,
}: {
  isOpen: boolean;
  onClose: () => void;
  guildId: string;
}) {
  const inviteToGuild = useInviteToGuild();
  const [search, setSearch] = useState('');
  const debouncedSearch = useDebounce(search, 400);
  const [players, setPlayers] = useState<{ userId: string; username: string; avatarUrl: string | null }[]>([]);
  const [isSearching, setIsSearching] = useState(false);
  const [invitedIds, setInvitedIds] = useState<Set<string>>(new Set());

  const searchPlayers = async (query: string) => {
    if (!query || query.length < 2) { setPlayers([]); return; }
    setIsSearching(true);
    try {
      const result = await discoverPlayers({ search: query, page: 1, pageSize: 10 });
      setPlayers(result.items.map((p) => ({
        userId: p.id,
        username: p.username,
        avatarUrl: p.avatarUrl,
      })));
    } catch {
      setPlayers([]);
    }
    setIsSearching(false);
  };

  const handleSearch = (value: string) => {
    setSearch(value);
  };

  useState(() => {
    if (debouncedSearch) searchPlayers(debouncedSearch);
    else setPlayers([]);
  });

  const handleInvite = (targetUserId: string) => {
    inviteToGuild.mutate(
      { guildId, userId: targetUserId },
      {
        onSuccess: () => {
          setInvitedIds((prev) => new Set(prev).add(targetUserId));
        },
      },
    );
  };

  return (
    <Modal isOpen={isOpen} onClose={onClose} title="Invite Player" className="max-w-md">
      <div className="space-y-4">
        <Input
          id="playerSearch"
          placeholder="Search by username..."
          value={search}
          onChange={(e) => {
            handleSearch(e.target.value);
            searchPlayers(e.target.value);
          }}
        />

        {isSearching && (
          <div className="flex justify-center py-4"><Spinner /></div>
        )}

        <AnimatePresence>
          {players.length > 0 && (
            <motion.div
              initial={{ opacity: 0 }}
              animate={{ opacity: 1 }}
              exit={{ opacity: 0 }}
              className="max-h-64 space-y-1 overflow-y-auto"
            >
              {players.map((player) => (
                <div
                  key={player.userId}
                  className="flex items-center gap-3 rounded-lg px-3 py-2 hover:bg-surface-hover transition-colors"
                >
                  <UserAvatar username={player.username} avatarUrl={player.avatarUrl} size="sm" />
                  <span className="flex-1 truncate text-sm font-medium text-foreground">{player.username}</span>
                  {invitedIds.has(player.userId) ? (
                    <span className="text-xs text-success font-medium">Invited</span>
                  ) : (
                    <Button
                      size="sm"
                      variant="outline"
                      onClick={() => handleInvite(player.userId)}
                      isLoading={inviteToGuild.isPending}
                    >
                      Invite
                    </Button>
                  )}
                </div>
              ))}
            </motion.div>
          )}
        </AnimatePresence>

        {!isSearching && search.length >= 2 && players.length === 0 && (
          <p className="text-center text-sm text-foreground-muted py-4">No players found</p>
        )}
      </div>
    </Modal>
  );
}
