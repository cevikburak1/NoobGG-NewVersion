import { useState, useMemo } from 'react';
import { useParams, useNavigate, Link } from 'react-router-dom';
import { motion, AnimatePresence } from 'framer-motion';
import { useRoomDetail, useJoinRoom, useLeaveRoom, useCloseRoom, useInviteToRoom } from '@/features/rooms/hooks';
import { useChatConnection, useChatHistory } from '@/features/chat/hooks';
import { useBlockedUsers } from '@/features/blocks/hooks';
import { useAuthStore } from '@/stores/authStore';
import { Button, Badge, Card, AnimatedPage, Spinner, Modal } from '@/components/ui';
import { UserAvatar } from '@/components/common/userAvatar';
import { ChatPanel } from '@/components/chat';
import { discoverPlayers } from '@/features/users/api';
import type { DiscoverPlayerResponse } from '@/features/users/types';
import { useToast } from '@/components/ui/toast';

const statusColors: Record<string, 'success' | 'warning' | 'primary' | 'danger' | 'default'> = {
  Open: 'success',
  Full: 'warning',
  InProgress: 'primary',
  Closed: 'danger',
};

export default function RoomDetailPage() {
  const { roomId } = useParams<{ roomId: string }>();
  const navigate = useNavigate();
  const user = useAuthStore((s) => s.user);

  const { data: room, isLoading } = useRoomDetail(roomId);
  const joinRoom = useJoinRoom();
  const leaveRoom = useLeaveRoom();
  const closeRoom = useCloseRoom();

  const inviteToRoom = useInviteToRoom();
  const { addToast } = useToast();

  const [showLeaveModal, setShowLeaveModal] = useState(false);
  const [showCloseModal, setShowCloseModal] = useState(false);
  const [showInviteModal, setShowInviteModal] = useState(false);

  const isMember = room?.members.some((m) => m.userId === user?.id);

  const chatRoomId = isMember ? roomId : undefined;
  const { data: history, isLoading: historyLoading } = useChatHistory(chatRoomId ?? '');
  const chat = useChatConnection(chatRoomId);
  const isOwner = room?.creatorId === user?.id;

  const { data: blockedUsers } = useBlockedUsers();
  const blockedIds = useMemo(
    () => new Set(blockedUsers?.map((b) => b.blockedUserId) ?? []),
    [blockedUsers],
  );

  const historyMessages = history?.items ?? [];
  const allMessages = [
    ...historyMessages,
    ...chat.messages.filter((m) => !historyMessages.some((h) => h.id === m.id)),
  ].filter((m) => !blockedIds.has(m.senderId));

  if (isLoading) {
    return (
      <div className="flex items-center justify-center py-32">
        <Spinner size="lg" />
      </div>
    );
  }

  if (!room) {
    return (
      <AnimatedPage>
        <div className="flex flex-col items-center py-32 text-center">
          <div className="text-5xl">🚪</div>
          <h2 className="mt-4 text-xl font-bold text-foreground">Room not found</h2>
          <p className="mt-2 text-foreground-muted">This room may have been closed or deleted.</p>
          <Link to="/rooms" className="mt-4">
            <Button variant="outline">Browse Rooms</Button>
          </Link>
        </div>
      </AnimatedPage>
    );
  }

  const handleJoin = () => {
    joinRoom.mutate(roomId!);
  };

  const handleLeave = () => {
    leaveRoom.mutate(roomId!, {
      onSuccess: () => {
        setShowLeaveModal(false);
        navigate('/rooms');
      },
    });
  };

  const handleCloseRoom = () => {
    closeRoom.mutate(roomId!, {
      onSuccess: () => {
        setShowCloseModal(false);
        navigate('/rooms');
      },
    });
  };

  return (
    <AnimatedPage>
      <div className="space-y-6">
        {/* Back link */}
        <motion.div initial={{ opacity: 0, x: -10 }} animate={{ opacity: 1, x: 0 }}>
          <Link
            to="/rooms"
            className="inline-flex items-center gap-1.5 rounded-lg px-2 py-1 text-sm font-medium text-foreground-muted transition-colors hover:bg-surface-hover hover:text-foreground"
          >
            <svg className="h-4 w-4" fill="none" stroke="currentColor" viewBox="0 0 24 24" strokeWidth={2}>
              <path strokeLinecap="round" strokeLinejoin="round" d="M15.75 19.5L8.25 12l7.5-7.5" />
            </svg>
            Rooms
          </Link>
        </motion.div>

        <div className="grid gap-6 lg:grid-cols-3">
          {/* Main content */}
          <div className="space-y-6 lg:col-span-2">
            <RoomHeader
              room={room}
              isMember={isMember}
              isOwner={isOwner}
              onJoin={handleJoin}
              joinPending={joinRoom.isPending}
              onLeave={() => setShowLeaveModal(true)}
              onClose={() => setShowCloseModal(true)}
            />

            {isMember ? (
              <ChatPanel
                messages={allMessages}
                currentUserId={user?.id ?? ''}
                status={chat.status}
                reconnectAttempt={chat.reconnectAttempt}
                typingUsers={chat.typingUsers}
                onlineUsers={chat.onlineUsers}
                onSendMessage={chat.sendMessage}
                onDeleteMessage={chat.deleteMessage}
                onTypingStart={chat.startTyping}
                onTypingStop={chat.stopTyping}
                hasMore={history?.hasPreviousPage}
                isLoadingMore={historyLoading}
              />
            ) : (
              <Card className="flex flex-col items-center py-14 text-center">
                <div className="flex h-14 w-14 items-center justify-center rounded-2xl bg-surface-hover/80">
                  <span className="text-2xl">💬</span>
                </div>
                <p className="mt-4 text-lg font-semibold text-foreground">Join to chat</p>
                <p className="mt-1.5 text-sm text-foreground-muted">
                  You need to be a member to access the chat
                </p>
                {room.status === 'Open' && (
                  <div className="mt-5">
                    <Button onClick={handleJoin} isLoading={joinRoom.isPending}>
                      Join Room
                    </Button>
                  </div>
                )}
              </Card>
            )}
          </div>

          {/* Sidebar */}
          <RoomSidebar
            room={room}
            onlineUsers={chat.onlineUsers}
            isMember={isMember}
            blockedIds={blockedIds}
            onInvite={() => setShowInviteModal(true)}
          />
        </div>

        <Modal isOpen={showLeaveModal} onClose={() => setShowLeaveModal(false)} title="Leave Room">
          <p className="text-sm text-foreground-muted">
            Are you sure you want to leave this room? You&apos;ll lose access to the chat.
          </p>
          <div className="mt-4 flex justify-end gap-3">
            <Button variant="ghost" onClick={() => setShowLeaveModal(false)}>Cancel</Button>
            <Button variant="danger" onClick={handleLeave} isLoading={leaveRoom.isPending}>Leave</Button>
          </div>
        </Modal>

        <Modal isOpen={showCloseModal} onClose={() => setShowCloseModal(false)} title="Close Room">
          <p className="text-sm text-foreground-muted">
            Are you sure you want to close this room? This will permanently delete the room, all members will be removed, and all chat messages will be lost.
          </p>
          <div className="mt-4 flex justify-end gap-3">
            <Button variant="ghost" onClick={() => setShowCloseModal(false)}>Cancel</Button>
            <Button variant="danger" onClick={handleCloseRoom} isLoading={closeRoom.isPending}>
              Close Room
            </Button>
          </div>
        </Modal>

        {showInviteModal && roomId && (
          <InvitePlayerModal
            roomId={roomId}
            memberIds={room?.members.map((m) => m.userId) ?? []}
            inviteMutation={inviteToRoom}
            onClose={() => setShowInviteModal(false)}
            addToast={addToast}
          />
        )}
      </div>
    </AnimatedPage>
  );
}

/* ─── Room header ─── */

function RoomHeader({
  room,
  isMember,
  isOwner,
  onJoin,
  joinPending,
  onLeave,
  onClose,
}: {
  room: { title: string; status: string; description: string | null; region: string; language: string; tags: string[]; currentMemberCount: number; maxMembers: number };
  isMember: boolean | undefined;
  isOwner: boolean | undefined;
  onJoin: () => void;
  joinPending: boolean;
  onLeave: () => void;
  onClose: () => void;
}) {
  const capacityPct = (room.currentMemberCount / room.maxMembers) * 100;

  return (
    <motion.div
      initial={{ opacity: 0, y: 20 }}
      animate={{ opacity: 1, y: 0 }}
      className="rounded-xl border border-border bg-surface p-6"
    >
      <div className="flex flex-col gap-4 sm:flex-row sm:items-start sm:justify-between">
        <div className="min-w-0 flex-1">
          <div className="flex items-center gap-3">
            <h1 className="truncate text-2xl font-bold text-foreground">{room.title}</h1>
            <Badge variant={statusColors[room.status] ?? 'default'}>{room.status}</Badge>
          </div>
          {room.description && (
            <p className="mt-2 text-sm text-foreground-muted">{room.description}</p>
          )}
          <div className="mt-3 flex flex-wrap gap-2">
            <Badge>{room.region}</Badge>
            <Badge>{room.language}</Badge>
            {room.tags.map((tag) => (
              <Badge key={tag} variant="primary">{tag}</Badge>
            ))}
          </div>
        </div>

        <div className="flex shrink-0 gap-2">
          {!isMember && room.status === 'Open' && (
            <motion.div whileHover={{ scale: 1.05 }} whileTap={{ scale: 0.98 }}>
              <Button onClick={onJoin} isLoading={joinPending}>Join Room</Button>
            </motion.div>
          )}
          {isMember && !isOwner && (
            <Button variant="danger" size="sm" onClick={onLeave}>Leave</Button>
          )}
          {isOwner && (
            <Button variant="danger" size="sm" onClick={onClose}>Close Room</Button>
          )}
        </div>
      </div>

      <div className="mt-4">
        <div className="flex items-center justify-between text-xs text-foreground-muted">
          <span>{room.currentMemberCount} / {room.maxMembers} members</span>
          <span>{Math.round(capacityPct)}%</span>
        </div>
        <div className="mt-1.5 h-2 w-full overflow-hidden rounded-full bg-surface-hover">
          <motion.div
            initial={{ width: 0 }}
            animate={{ width: `${capacityPct}%` }}
            transition={{ duration: 0.8 }}
            className={`h-full rounded-full ${
              capacityPct >= 100 ? 'bg-danger' : capacityPct >= 80 ? 'bg-warning' : 'bg-accent'
            }`}
          />
        </div>
      </div>
    </motion.div>
  );
}

/* ─── Sidebar ─── */

function RoomSidebar({
  room,
  onlineUsers,
  isMember,
  blockedIds,
  onInvite,
}: {
  room: { members: { userId: string; username: string; role: string; joinedAt: string }[]; createdAt: string; isPublic: boolean; rankRange: { min: string; max: string } | null; status: string };
  onlineUsers: { userId: string; username: string }[];
  isMember: boolean | undefined;
  blockedIds: Set<string>;
  onInvite: () => void;
}) {
  const onlineIds = new Set(onlineUsers.map((u) => u.userId));

  return (
    <motion.div
      initial={{ opacity: 0, x: 20 }}
      animate={{ opacity: 1, x: 0 }}
      transition={{ delay: 0.15 }}
      className="space-y-4"
    >
      {isMember && onlineUsers.length > 0 && (
        <Card>
          <h3 className="mb-3 flex items-center gap-2 text-xs font-semibold uppercase tracking-wider text-foreground-muted">
            <span className="h-2 w-2 rounded-full bg-success animate-pulse" />
            Online &middot; {onlineUsers.length}
          </h3>
          <div className="space-y-2">
            <AnimatePresence>
              {onlineUsers.map((u) => (
                <motion.div
                  key={u.userId}
                  initial={{ opacity: 0, x: 10 }}
                  animate={{ opacity: 1, x: 0 }}
                  exit={{ opacity: 0, x: -10 }}
                  className="flex items-center gap-2"
                >
                  <div className="relative">
                    <UserAvatar username={u.username} avatarUrl={u.avatarUrl} size="sm" />
                    <div className="absolute -bottom-0.5 -right-0.5 h-3 w-3 rounded-full border-2 border-surface bg-success" />
                  </div>
                  <Link
                    to={`/profile/${u.userId}`}
                    className="truncate text-sm text-foreground hover:text-primary transition-colors"
                  >
                    {u.username}
                  </Link>
                </motion.div>
              ))}
            </AnimatePresence>
          </div>
        </Card>
      )}

      {/* All members */}
      <Card>
        <div className="mb-3 flex items-center justify-between">
          <h3 className="text-xs font-semibold uppercase tracking-wider text-foreground-muted">
            Members &middot; {room.members.length}
          </h3>
          {isMember && room.status !== 'Closed' && (
            <button
              onClick={onInvite}
              className="flex items-center gap-1 rounded-md px-2 py-1 text-xs font-medium text-primary hover:bg-primary/10 transition-colors"
            >
              <svg className="h-3.5 w-3.5" fill="none" stroke="currentColor" viewBox="0 0 24 24" strokeWidth={2}>
                <path strokeLinecap="round" strokeLinejoin="round" d="M19 7.5v3m0 0v3m0-3h3m-3 0h-3m-2.25-4.125a3.375 3.375 0 11-6.75 0 3.375 3.375 0 016.75 0zM4 19.235v-.11a6.375 6.375 0 0112.75 0v.109A12.318 12.318 0 0110.374 21c-2.331 0-4.512-.645-6.374-1.766z" />
              </svg>
              Invite
            </button>
          )}
        </div>
        <div className="space-y-2">
          {room.members.map((member, i) => (
            <motion.div
              key={member.userId}
              initial={{ opacity: 0, y: 10 }}
              animate={{ opacity: 1, y: 0 }}
              transition={{ delay: i * 0.04 }}
              className="flex items-center gap-3"
            >
              <div className="relative">
                <UserAvatar username={member.username} avatarUrl={member.avatarUrl} size="sm" />
                {isMember && (
                  <div className={`absolute -bottom-0.5 -right-0.5 h-2.5 w-2.5 rounded-full border-2 border-surface ${
                    blockedIds.has(member.userId) ? 'bg-foreground-subtle' : onlineIds.has(member.userId) ? 'bg-success' : 'bg-danger'
                  }`} />
                )}
              </div>
              <div className="min-w-0 flex-1">
                <Link
                  to={`/profile/${member.userId}`}
                  className="block truncate text-sm font-medium text-foreground hover:text-primary transition-colors"
                >
                  {member.username}
                </Link>
                <span className="text-[10px] text-foreground-subtle">
                  Joined {new Date(member.joinedAt).toLocaleDateString()}
                </span>
              </div>
              {blockedIds.has(member.userId) ? (
                <Badge variant="danger" className="shrink-0">Blocked</Badge>
              ) : member.role === 'Owner' ? (
                <Badge variant="warning" className="shrink-0">Owner</Badge>
              ) : null}
            </motion.div>
          ))}
        </div>
      </Card>

      {/* Room info */}
      <Card>
        <h3 className="mb-3 text-xs font-semibold uppercase tracking-wider text-foreground-muted">Room Info</h3>
        <div className="space-y-2 text-sm">
          <InfoRow label="Created" value={new Date(room.createdAt).toLocaleDateString()} />
          <InfoRow label="Visibility" value={room.isPublic ? 'Public' : 'Private'} />
          {room.rankRange && (
            <InfoRow label="Rank" value={`${room.rankRange.min} – ${room.rankRange.max}`} />
          )}
        </div>
      </Card>
    </motion.div>
  );
}

function InfoRow({ label, value }: { label: string; value: string }) {
  return (
    <div className="flex items-center justify-between">
      <span className="text-foreground-subtle">{label}</span>
      <span className="text-foreground">{value}</span>
    </div>
  );
}

/* ─── Invite Player Modal ─── */

function InvitePlayerModal({
  roomId,
  memberIds,
  inviteMutation,
  onClose,
  addToast,
}: {
  roomId: string;
  memberIds: string[];
  inviteMutation: ReturnType<typeof useInviteToRoom>;
  onClose: () => void;
  addToast: (t: { title: string; type: 'success' | 'error' | 'info' }) => void;
}) {
  const [search, setSearch] = useState('');
  const [results, setResults] = useState<DiscoverPlayerResponse[]>([]);
  const [searching, setSearching] = useState(false);
  const [invitedIds, setInvitedIds] = useState<Set<string>>(new Set());
  const memberSet = useMemo(() => new Set(memberIds), [memberIds]);

  const handleSearch = async () => {
    if (search.trim().length < 2) return;
    setSearching(true);
    try {
      const data = await discoverPlayers({ search: search.trim(), pageSize: 10 });
      setResults(data.items.filter((p) => !memberSet.has(p.id)));
    } catch {
      setResults([]);
    } finally {
      setSearching(false);
    }
  };

  const handleInvite = async (userId: string) => {
    try {
      await inviteMutation.mutateAsync({ roomId, userId });
      setInvitedIds((prev) => new Set(prev).add(userId));
      addToast({ title: 'Invite sent!', type: 'success' });
    } catch {
      addToast({ title: 'Could not send invite', type: 'error' });
    }
  };

  return (
    <Modal isOpen onClose={onClose} title="Invite Player">
      <div className="space-y-4">
        <div className="flex gap-2">
          <input
            type="text"
            placeholder="Search by username..."
            value={search}
            onChange={(e) => setSearch(e.target.value)}
            onKeyDown={(e) => e.key === 'Enter' && handleSearch()}
            className="flex-1 rounded-lg border border-border bg-surface-hover px-3 py-2 text-sm text-foreground placeholder:text-foreground-subtle focus:outline-none focus:ring-2 focus:ring-primary/50"
            autoFocus
          />
          <Button size="sm" onClick={handleSearch} isLoading={searching}>
            Search
          </Button>
        </div>

        {searching && (
          <div className="flex justify-center py-4">
            <Spinner size="sm" />
          </div>
        )}

        {!searching && results.length > 0 && (
          <div className="max-h-60 space-y-2 overflow-y-auto">
            {results.map((player) => (
              <div key={player.id} className="flex items-center gap-3 rounded-lg border border-border p-2">
                <UserAvatar username={player.username} avatarUrl={player.avatarUrl} size="sm" />
                <div className="min-w-0 flex-1">
                  <p className="truncate text-sm font-medium text-foreground">{player.username}</p>
                  {player.region && (
                    <p className="text-xs text-foreground-muted">{player.region}</p>
                  )}
                </div>
                {invitedIds.has(player.id) ? (
                  <Badge variant="success">Invited</Badge>
                ) : (
                  <Button
                    size="sm"
                    variant="outline"
                    onClick={() => handleInvite(player.id)}
                    isLoading={inviteMutation.isPending}
                  >
                    Invite
                  </Button>
                )}
              </div>
            ))}
          </div>
        )}

        {!searching && results.length === 0 && search.length >= 2 && (
          <p className="text-center text-sm text-foreground-muted py-4">No players found</p>
        )}
      </div>
    </Modal>
  );
}
