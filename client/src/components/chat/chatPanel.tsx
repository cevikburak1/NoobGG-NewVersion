import { motion } from 'framer-motion';
import type { ChatMessageResponse } from '@/features/chat/types';
import type { ConnectionStatus } from '@/lib/signalr';
import type { TypingUser } from '@/features/chat/hooks';
import type { OnlineUser } from '@/features/chat/types';
import { ConnectionBanner } from './connectionBanner';
import { ChatHeader } from './chatHeader';
import { ChatMessages } from './chatMessages';
import { ChatInput } from './chatInput';

interface ChatPanelProps {
  messages: ChatMessageResponse[];
  currentUserId: string;
  status: ConnectionStatus;
  reconnectAttempt: number;
  typingUsers: TypingUser[];
  onlineUsers: OnlineUser[];
  onSendMessage: (content: string) => Promise<void>;
  onDeleteMessage?: (messageId: string) => Promise<void>;
  onTypingStart: () => Promise<void>;
  onTypingStop: () => Promise<void>;
  onLoadMore?: () => void;
  hasMore?: boolean;
  isLoadingMore?: boolean;
  className?: string;
}

export function ChatPanel({
  messages,
  currentUserId,
  status,
  reconnectAttempt,
  typingUsers,
  onlineUsers,
  onSendMessage,
  onDeleteMessage,
  onTypingStart,
  onTypingStop,
  onLoadMore,
  hasMore,
  isLoadingMore,
  className,
}: ChatPanelProps) {
  return (
    <motion.div
      initial={{ opacity: 0, y: 20 }}
      animate={{ opacity: 1, y: 0 }}
      transition={{ delay: 0.2 }}
      className={`flex flex-col overflow-hidden rounded-xl border border-border bg-surface ${className ?? 'h-[500px]'}`}
    >
      <ChatHeader status={status} typingUsers={typingUsers} onlineUsers={onlineUsers} />
      <ConnectionBanner status={status} reconnectAttempt={reconnectAttempt} />
      <ChatMessages
        messages={messages}
        currentUserId={currentUserId}
        onDeleteMessage={onDeleteMessage}
        onLoadMore={onLoadMore}
        hasMore={hasMore}
        isLoadingMore={isLoadingMore}
      />
      <ChatInput
        onSend={onSendMessage}
        onTypingStart={onTypingStart}
        onTypingStop={onTypingStop}
        status={status}
      />
    </motion.div>
  );
}
