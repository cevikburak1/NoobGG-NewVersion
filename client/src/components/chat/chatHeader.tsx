import { motion } from 'framer-motion';
import type { ConnectionStatus } from '@/lib/signalr';
import type { TypingUser } from '@/features/chat/hooks';
import type { OnlineUser } from '@/features/chat/types';

interface ChatHeaderProps {
  status: ConnectionStatus;
  typingUsers: TypingUser[];
  onlineUsers: OnlineUser[];
}

export function ChatHeader({ status, typingUsers, onlineUsers }: ChatHeaderProps) {
  return (
    <div className="flex items-center justify-between border-b border-border px-4 py-3">
      <div className="flex items-center gap-2">
        <h2 className="font-semibold text-foreground">Chat</h2>
        <StatusDot status={status} />
        {status === 'connected' && onlineUsers.length > 0 && (
          <span className="text-xs text-foreground-subtle">
            {onlineUsers.length} online
          </span>
        )}
      </div>
      <TypingIndicator users={typingUsers} />
    </div>
  );
}

function StatusDot({ status }: { status: ConnectionStatus }) {
  const colors: Record<ConnectionStatus, string> = {
    connected: 'bg-success',
    connecting: 'bg-warning',
    reconnecting: 'bg-warning',
    disconnected: 'bg-danger',
  };

  return (
    <div className="relative flex items-center">
      <span className={`h-2 w-2 rounded-full ${colors[status]}`} />
      {(status === 'connecting' || status === 'reconnecting') && (
        <motion.span
          animate={{ scale: [1, 2], opacity: [0.6, 0] }}
          transition={{ duration: 1, repeat: Infinity }}
          className={`absolute h-2 w-2 rounded-full ${colors[status]}`}
        />
      )}
    </div>
  );
}

function TypingIndicator({ users }: { users: TypingUser[] }) {
  if (users.length === 0) return null;

  const text =
    users.length === 1
      ? `${users[0].username} is typing`
      : users.length === 2
        ? `${users[0].username} and ${users[1].username} are typing`
        : `${users[0].username} and ${users.length - 1} others are typing`;

  return (
    <motion.div
      initial={{ opacity: 0, x: 10 }}
      animate={{ opacity: 1, x: 0 }}
      exit={{ opacity: 0 }}
      className="flex items-center gap-1.5 text-xs text-foreground-muted"
    >
      <span>{text}</span>
      <BouncingDots />
    </motion.div>
  );
}

function BouncingDots() {
  return (
    <span className="inline-flex gap-0.5">
      {[0, 1, 2].map((i) => (
        <motion.span
          key={i}
          animate={{ y: [0, -3, 0] }}
          transition={{ duration: 0.5, repeat: Infinity, delay: i * 0.15 }}
          className="inline-block h-1 w-1 rounded-full bg-foreground-muted"
        />
      ))}
    </span>
  );
}
