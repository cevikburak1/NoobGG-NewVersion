import { useState } from 'react';
import { useParams, useNavigate, Link } from 'react-router-dom';
import { motion, AnimatePresence } from 'framer-motion';
import { useRoomDetail, useJoinRoom, useLeaveRoom, useCloseRoom } from '@/features/rooms/hooks';
import { useChatConnection, useChatHistory } from '@/features/chat/hooks';
import { useAuthStore } from '@/stores/authStore';
import { Button, Badge, Card, AnimatedPage, Spinner, Modal } from '@/components/ui';
import { UserAvatar } from '@/components/common/userAvatar';
import { ChatPanel } from '@/components/chat';

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

  const [showLeaveModal, setShowLeaveModal] = useState(false);
  const [showCloseModal, setShowCloseModal] = useState(false);

  const isMember = room?.members.some((m) => m.userId === user?.id);

  const chatRoomId = isMember ? roomId : undefined;
  const { data: history, isLoading: historyLoading } = useChatHistory(chatRoomId ?? '');
  const chat = useChatConnection(chatRoomId);
  const isOwner = room?.creatorId === user?.id;

  const historyMessages = history?.items ?? [];
  const allMessages = [
    ...historyMessages,
    ...chat.messages.filter((m) => !historyMessages.some((h) => h.id === m.id)),
  ];

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
            className="inline-flex items-center gap-1 text-sm text-foreground-muted hover:text-foreground transition-colors"
          >
            <svg className="h-4 w-4" fill="none" stroke="currentColor" viewBox="0 0 24 24" strokeWidth={2}>
              <path strokeLinecap="round" strokeLinejoin="round" d="M15.75 19.5L8.25 12l7.5-7.5" />
            </svg>
            Back to Rooms
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
              <Card className="py-12 text-center">
                <motion.div
                  initial={{ scale: 0 }}
                  animate={{ scale: 1 }}
                  transition={{ type: 'spring', bounce: 0.5 }}
                  className="text-4xl"
                >
                  💬
                </motion.div>
                <p className="mt-3 text-lg font-semibold text-foreground">Join to chat</p>
                <p className="mt-1 text-sm text-foreground-muted">
                  You need to be a member to access the chat
                </p>
                {room.status === 'Open' && (
                  <motion.div whileHover={{ scale: 1.05 }} whileTap={{ scale: 0.98 }} className="mt-4">
                    <Button onClick={handleJoin} isLoading={joinRoom.isPending}>
                      Join Room
                    </Button>
                  </motion.div>
                )}
              </Card>
            )}
          </div>

          {/* Sidebar */}
          <RoomSidebar
            room={room}
            onlineUsers={chat.onlineUsers}
            isMember={isMember}
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
}: {
  room: { members: { userId: string; username: string; role: string; joinedAt: string }[]; createdAt: string; isPublic: boolean; rankRange: { min: string; max: string } | null };
  onlineUsers: { userId: string; username: string }[];
  isMember: boolean | undefined;
}) {
  const onlineIds = new Set(onlineUsers.map((u) => u.userId));

  return (
    <motion.div
      initial={{ opacity: 0, x: 20 }}
      animate={{ opacity: 1, x: 0 }}
      transition={{ delay: 0.15 }}
      className="space-y-4"
    >
      {/* Online */}
      {isMember && onlineUsers.length > 0 && (
        <Card>
          <h3 className="mb-3 flex items-center gap-2 text-sm font-semibold text-foreground">
            <span className="h-2 w-2 rounded-full bg-success" />
            Online ({onlineUsers.length})
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
                    <UserAvatar username={u.username} size="sm" />
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
        <h3 className="mb-3 text-sm font-semibold text-foreground">
          Members ({room.members.length})
        </h3>
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
                <UserAvatar username={member.username} size="sm" />
                {isMember && (
                  <div className={`absolute -bottom-0.5 -right-0.5 h-2.5 w-2.5 rounded-full border-2 border-surface ${
                    onlineIds.has(member.userId) ? 'bg-success' : 'bg-danger'
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
              {member.role === 'Owner' && (
                <Badge variant="warning" className="shrink-0">Owner</Badge>
              )}
            </motion.div>
          ))}
        </div>
      </Card>

      {/* Room info */}
      <Card>
        <h3 className="mb-3 text-sm font-semibold text-foreground">Room Info</h3>
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
