import { useRef, useEffect, useState, useCallback, type ReactNode } from 'react';
import { motion, AnimatePresence } from 'framer-motion';
import type { ChatMessageResponse } from '@/features/chat/types';
import { UserAvatar } from '@/components/common/userAvatar';

interface ChatMessagesProps {
  messages: ChatMessageResponse[];
  currentUserId: string;
  onDeleteMessage?: (messageId: string) => Promise<void>;
  onLoadMore?: () => void;
  hasMore?: boolean;
  isLoadingMore?: boolean;
}

export function ChatMessages({
  messages,
  currentUserId,
  onDeleteMessage,
  onLoadMore,
  hasMore,
  isLoadingMore,
}: ChatMessagesProps) {
  const containerRef = useRef<HTMLDivElement>(null);
  const endRef = useRef<HTMLDivElement>(null);
  const [isAtBottom, setIsAtBottom] = useState(true);
  const [unreadCount, setUnreadCount] = useState(0);
  const prevLengthRef = useRef(messages.length);

  const scrollToBottom = useCallback((smooth = true) => {
    endRef.current?.scrollIntoView({ behavior: smooth ? 'smooth' : 'instant' });
    setUnreadCount(0);
  }, []);

  useEffect(() => {
    if (messages.length > prevLengthRef.current) {
      if (isAtBottom) {
        scrollToBottom();
      } else {
        setUnreadCount((n) => n + (messages.length - prevLengthRef.current));
      }
    }
    prevLengthRef.current = messages.length;
  }, [messages.length, isAtBottom, scrollToBottom]);

  useEffect(() => {
    scrollToBottom(false);
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, []);

  const handleScroll = () => {
    const el = containerRef.current;
    if (!el) return;

    const atBottom = el.scrollHeight - el.scrollTop - el.clientHeight < 60;
    setIsAtBottom(atBottom);
    if (atBottom) setUnreadCount(0);

    if (el.scrollTop < 80 && hasMore && !isLoadingMore && onLoadMore) {
      onLoadMore();
    }
  };

  const grouped = groupMessages(messages);

  return (
    <div className="relative flex-1 overflow-hidden">
      <div
        ref={containerRef}
        onScroll={handleScroll}
        className="h-full overflow-y-auto px-4 py-3"
      >
        {/* Load more */}
        {hasMore && (
          <div className="mb-3 flex justify-center">
            {isLoadingMore ? (
              <motion.div
                animate={{ rotate: 360 }}
                transition={{ duration: 1, repeat: Infinity, ease: 'linear' }}
                className="h-5 w-5 rounded-full border-2 border-primary border-t-transparent"
              />
            ) : (
              <button
                onClick={onLoadMore}
                className="rounded-md px-3 py-1 text-xs text-foreground-muted hover:bg-surface-hover transition-colors"
              >
                Load older messages
              </button>
            )}
          </div>
        )}

        {messages.length === 0 && (
          <div className="flex h-full flex-col items-center justify-center gap-2 text-center">
            <motion.div
              initial={{ scale: 0 }}
              animate={{ scale: 1 }}
              transition={{ type: 'spring', bounce: 0.5 }}
              className="text-4xl"
            >
              💬
            </motion.div>
            <p className="text-sm text-foreground-subtle">No messages yet. Start the conversation!</p>
          </div>
        )}

        {grouped.map((group) => (
          <div key={group.key}>
            {group.dateSeparator && <DateSeparator date={group.dateSeparator} />}
            <MessageGroup
              messages={group.messages}
              currentUserId={currentUserId}
              onDelete={onDeleteMessage}
            />
          </div>
        ))}

        <div ref={endRef} />
      </div>

      {/* Scroll to bottom FAB */}
      <AnimatePresence>
        {!isAtBottom && (
          <motion.button
            initial={{ opacity: 0, y: 10, scale: 0.9 }}
            animate={{ opacity: 1, y: 0, scale: 1 }}
            exit={{ opacity: 0, y: 10, scale: 0.9 }}
            onClick={() => scrollToBottom()}
            className="absolute bottom-3 right-3 flex items-center gap-1.5 rounded-full border border-border bg-surface px-3 py-1.5 text-xs font-medium text-foreground shadow-lg hover:bg-surface-hover transition-colors"
          >
            <svg className="h-3 w-3" fill="none" stroke="currentColor" viewBox="0 0 24 24" strokeWidth={2}>
              <path strokeLinecap="round" strokeLinejoin="round" d="M19 14l-7 7m0 0l-7-7m7 7V3" />
            </svg>
            {unreadCount > 0 ? `${unreadCount} new` : 'Bottom'}
          </motion.button>
        )}
      </AnimatePresence>
    </div>
  );
}

/* ─── Date separator ─── */

function DateSeparator({ date }: { date: string }) {
  return (
    <div className="my-4 flex items-center gap-3">
      <div className="h-px flex-1 bg-border" />
      <span className="shrink-0 text-[10px] font-medium uppercase tracking-wider text-foreground-subtle">
        {date}
      </span>
      <div className="h-px flex-1 bg-border" />
    </div>
  );
}

/* ─── Message group (consecutive msgs from same sender) ─── */

function MessageGroup({
  messages,
  currentUserId,
  onDelete,
}: {
  messages: ChatMessageResponse[];
  currentUserId: string;
  onDelete?: (id: string) => Promise<void>;
}) {
  const first = messages[0];
  const isOwn = first.senderId === currentUserId;
  const isSystem = first.type === 'System';

  if (isSystem) {
    return (
      <>
        {messages.map((msg) => (
          <motion.div
            key={msg.id}
            initial={{ opacity: 0 }}
            animate={{ opacity: 1 }}
            className="my-2 text-center text-xs text-foreground-subtle"
          >
            {msg.content}
          </motion.div>
        ))}
      </>
    );
  }

  return (
    <div className={`mb-3 flex gap-2.5 ${isOwn ? 'flex-row-reverse' : ''}`}>
      {!isOwn && (
        <div className="mt-0.5 shrink-0">
          <UserAvatar username={first.senderUsername} size="sm" className="!h-7 !w-7 !text-[10px]" />
        </div>
      )}
      <div className={`min-w-0 max-w-[75%] ${isOwn ? 'items-end' : 'items-start'} flex flex-col`}>
        {!isOwn && (
          <span className="mb-0.5 text-xs font-medium text-primary">{first.senderUsername}</span>
        )}
        {messages.map((msg, i) => (
          <MessageBubble
            key={msg.id}
            message={msg}
            isOwn={isOwn}
            isFirst={i === 0}
            isLast={i === messages.length - 1}
            onDelete={isOwn && onDelete ? () => onDelete(msg.id) : undefined}
          />
        ))}
      </div>
    </div>
  );
}

/* ─── Single bubble ─── */

function MessageBubble({
  message,
  isOwn,
  isFirst,
  isLast,
  onDelete,
}: {
  message: ChatMessageResponse;
  isOwn: boolean;
  isFirst: boolean;
  isLast: boolean;
  onDelete?: () => void;
}) {
  const [showActions, setShowActions] = useState(false);

  const roundedClass = isOwn
    ? `${isFirst ? 'rounded-tr-lg' : 'rounded-tr-md'} ${isLast ? 'rounded-br-md' : 'rounded-br-md'} rounded-tl-lg rounded-bl-lg`
    : `${isFirst ? 'rounded-tl-lg' : 'rounded-tl-md'} ${isLast ? 'rounded-bl-md' : 'rounded-bl-md'} rounded-tr-lg rounded-br-lg`;

  return (
    <motion.div
      initial={{ opacity: 0, y: 6, x: isOwn ? 6 : -6 }}
      animate={{ opacity: 1, y: 0, x: 0 }}
      transition={{ duration: 0.15 }}
      className={`group relative mb-0.5 ${isOwn ? 'self-end' : 'self-start'}`}
      onMouseEnter={() => setShowActions(true)}
      onMouseLeave={() => setShowActions(false)}
    >
      <div
        className={`${roundedClass} px-3 py-1.5 text-sm leading-relaxed ${
          isOwn
            ? 'bg-primary text-primary-foreground'
            : 'bg-surface-hover text-foreground'
        }`}
      >
        <span>{message.content}</span>
        {message.isEdited && (
          <span className={`ml-1.5 text-[10px] ${isOwn ? 'text-primary-foreground/60' : 'text-foreground-subtle'}`}>
            (edited)
          </span>
        )}
      </div>

      {isLast && (
        <span className={`mt-0.5 block text-[10px] text-foreground-subtle ${isOwn ? 'text-right' : ''}`}>
          {formatTime(message.createdAt)}
        </span>
      )}

      {/* Delete action */}
      <AnimatePresence>
        {showActions && onDelete && (
          <motion.button
            initial={{ opacity: 0, scale: 0.8 }}
            animate={{ opacity: 1, scale: 1 }}
            exit={{ opacity: 0, scale: 0.8 }}
            onClick={onDelete}
            className={`absolute top-0 ${isOwn ? '-left-7' : '-right-7'} rounded-md p-1 text-foreground-subtle hover:text-danger hover:bg-danger/10 transition-colors`}
            title="Delete"
          >
            <svg className="h-3.5 w-3.5" fill="none" stroke="currentColor" viewBox="0 0 24 24" strokeWidth={1.5}>
              <path strokeLinecap="round" strokeLinejoin="round" d="M14.74 9l-.346 9m-4.788 0L9.26 9m9.968-3.21c.342.052.682.107 1.022.166m-1.022-.165L18.16 19.673a2.25 2.25 0 01-2.244 2.077H8.084a2.25 2.25 0 01-2.244-2.077L4.772 5.79m14.456 0a48.108 48.108 0 00-3.478-.397m-12 .562c.34-.059.68-.114 1.022-.165m0 0a48.11 48.11 0 013.478-.397m7.5 0v-.916c0-1.18-.91-2.164-2.09-2.201a51.964 51.964 0 00-3.32 0c-1.18.037-2.09 1.022-2.09 2.201v.916m7.5 0a48.667 48.667 0 00-7.5 0" />
            </svg>
          </motion.button>
        )}
      </AnimatePresence>
    </motion.div>
  );
}

/* ─── Helpers ─── */

function formatTime(iso: string) {
  return new Date(iso).toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' });
}

function formatDateLabel(date: Date): string {
  const today = new Date();
  const yesterday = new Date(today);
  yesterday.setDate(yesterday.getDate() - 1);

  if (isSameDay(date, today)) return 'Today';
  if (isSameDay(date, yesterday)) return 'Yesterday';
  return date.toLocaleDateString(undefined, { month: 'short', day: 'numeric', year: 'numeric' });
}

function isSameDay(a: Date, b: Date) {
  return a.getFullYear() === b.getFullYear() && a.getMonth() === b.getMonth() && a.getDate() === b.getDate();
}

interface MessageGroupData {
  key: string;
  dateSeparator: string | null;
  messages: ChatMessageResponse[];
}

function groupMessages(messages: ChatMessageResponse[]): MessageGroupData[] {
  const groups: MessageGroupData[] = [];
  let lastDateLabel = '';

  for (let i = 0; i < messages.length; i++) {
    const msg = messages[i];
    const msgDate = new Date(msg.createdAt);
    const dateLabel = formatDateLabel(msgDate);
    const dateSeparator = dateLabel !== lastDateLabel ? dateLabel : null;
    if (dateSeparator) lastDateLabel = dateLabel;

    const prev = groups[groups.length - 1];
    const prevMsg = prev?.messages[prev.messages.length - 1];

    const canGroup =
      !dateSeparator &&
      prev &&
      prevMsg &&
      prevMsg.senderId === msg.senderId &&
      prevMsg.type === msg.type &&
      new Date(msg.createdAt).getTime() - new Date(prevMsg.createdAt).getTime() < 120_000;

    if (canGroup) {
      prev.messages.push(msg);
    } else {
      groups.push({
        key: `${msg.id}-${dateLabel}`,
        dateSeparator,
        messages: [msg],
      });
    }
  }

  return groups;
}

/* ─── Reusable sub-wrapper used externally ─── */

export function ChatEmptyPlaceholder({ children }: { children: ReactNode }) {
  return (
    <div className="flex h-full items-center justify-center">
      <div className="text-center">
        {children}
      </div>
    </div>
  );
}
