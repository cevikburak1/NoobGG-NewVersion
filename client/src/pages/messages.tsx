import { useState, useEffect, useRef } from 'react';
import { useSearchParams } from 'react-router-dom';
import { motion, AnimatePresence } from 'framer-motion';
import { useConversations, useMessages, useCreateConversation, useMarkRead } from '@/features/dm/hooks';
import { useDm } from '@/providers/dmProvider';
import { useAuthStore } from '@/stores/authStore';
import { usePresence } from '@/features/users/usePresence';
import { Button, Input, Spinner, AnimatedPage, Badge } from '@/components/ui';
import { UserAvatar } from '@/components/common/userAvatar';
import type { ConversationResponse } from '@/features/dm/types';

export default function MessagesPage() {
  const [searchParams] = useSearchParams();
  const targetUser = searchParams.get('user');

  const user = useAuthStore((s) => s.user);
  const { data: conversations, isLoading: convsLoading } = useConversations();
  const createConversation = useCreateConversation();
  const markRead = useMarkRead();
  const { sendMessage: sendViaHub, status: dmStatus } = useDm();

  const [activeConvId, setActiveConvId] = useState<string | null>(null);
  const [messageInput, setMessageInput] = useState('');
  const [isSending, setIsSending] = useState(false);
  const messagesEndRef = useRef<HTMLDivElement>(null);
  const conversationCreatedRef = useRef(false);

  const { data: messages, isLoading: msgsLoading } = useMessages(activeConvId);

  useEffect(() => {
    if (!targetUser || !conversations || activeConvId) return;

    const existing = conversations.find((c) => c.partnerId === targetUser);
    if (existing) {
      setActiveConvId(existing.id);
      return;
    }

    if (conversationCreatedRef.current) return;
    conversationCreatedRef.current = true;

    createConversation.mutate(
      { participantId: targetUser },
      { onSuccess: (conv) => setActiveConvId(conv.id) },
    );
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [targetUser, conversations, activeConvId]);

  useEffect(() => {
    if (activeConvId) {
      markRead.mutate(activeConvId);
    }
  }, [activeConvId]);

  useEffect(() => {
    messagesEndRef.current?.scrollIntoView({ behavior: 'smooth' });
  }, [messages]);

  const handleSend = async () => {
    if (!messageInput.trim() || !activeConvId) return;
    setIsSending(true);
    try {
      await sendViaHub(activeConvId, messageInput.trim());
      setMessageInput('');
    } catch {
      // fallback or error
    } finally {
      setIsSending(false);
    }
  };

  const handleKeyDown = (e: React.KeyboardEvent) => {
    if (e.key === 'Enter' && !e.shiftKey) {
      e.preventDefault();
      handleSend();
    }
  };

  const activeConv = conversations?.find((c) => c.id === activeConvId);
  const { data: partnerPresence } = usePresence(activeConv?.partnerId);

  return (
    <AnimatedPage className="h-[calc(100vh-4rem)]">
      <div className="flex h-full border border-border rounded-xl overflow-hidden bg-surface">
        <div className="w-80 border-r border-border flex flex-col shrink-0">
          <div className="p-4 border-b border-border">
            <h2 className="text-lg font-bold text-foreground">Messages</h2>
            <div className="flex items-center gap-2 mt-1">
              <div
                className={`w-2 h-2 rounded-full ${
                  dmStatus === 'connected'
                    ? 'bg-green-500'
                    : dmStatus === 'reconnecting'
                      ? 'bg-yellow-500 animate-pulse'
                      : 'bg-red-500'
                }`}
              />
              <span className="text-xs text-foreground-muted capitalize">{dmStatus}</span>
            </div>
          </div>

          <div className="flex-1 overflow-y-auto">
            {convsLoading ? (
              <div className="flex justify-center py-8">
                <Spinner />
              </div>
            ) : conversations && conversations.length > 0 ? (
              conversations.map((conv) => (
                <ConversationItem
                  key={conv.id}
                  conversation={conv}
                  isActive={activeConvId === conv.id}
                  currentUserId={user?.id ?? ''}
                  onClick={() => setActiveConvId(conv.id)}
                />
              ))
            ) : (
              <div className="p-4 text-center text-foreground-muted text-sm">
                No conversations yet. Visit a user's profile and click "Send Message" to start.
              </div>
            )}
          </div>
        </div>

        <div className="flex-1 flex flex-col min-w-0">
          {activeConv ? (
            <>
              <div className="p-4 border-b border-border flex items-center gap-3">
                <div className="relative">
                  <UserAvatar
                    username={activeConv.partnerUsername}
                    avatarUrl={activeConv.partnerAvatarUrl}
                    size="sm"
                  />
                  <span
                    className={`absolute bottom-0 right-0 h-3 w-3 rounded-full border-2 border-surface ${
                      partnerPresence?.isOnline ? 'bg-green-500' : 'bg-gray-500'
                    }`}
                  />
                </div>
                <div>
                  <p className="font-semibold text-foreground">{activeConv.partnerUsername}</p>
                  <p className={`text-xs ${partnerPresence?.isOnline ? 'text-green-500' : 'text-foreground-subtle'}`}>
                    {partnerPresence?.isOnline ? 'Online' : 'Offline'}
                  </p>
                </div>
              </div>

              <div className="flex-1 overflow-y-auto p-4 space-y-3">
                {msgsLoading ? (
                  <div className="flex justify-center py-8">
                    <Spinner />
                  </div>
                ) : messages && messages.length > 0 ? (
                  <>
                    {messages.map((msg) => {
                      const isOwn = msg.senderId === user?.id;
                      return (
                        <motion.div
                          key={msg.id}
                          initial={{ opacity: 0, y: 5 }}
                          animate={{ opacity: 1, y: 0 }}
                          className={`flex ${isOwn ? 'justify-end' : 'justify-start'}`}
                        >
                          <div
                            className={`max-w-[70%] rounded-2xl px-4 py-2 ${
                              isOwn
                                ? 'bg-primary text-primary-foreground rounded-br-md'
                                : 'bg-background border border-border text-foreground rounded-bl-md'
                            }`}
                          >
                            <p className="text-sm whitespace-pre-wrap wrap-break-word">{msg.content}</p>
                            <p
                              className={`text-xs mt-1 ${
                                isOwn ? 'text-primary-foreground/70' : 'text-foreground-muted'
                              }`}
                            >
                              {new Date(msg.createdAt).toLocaleTimeString([], {
                                hour: '2-digit',
                                minute: '2-digit',
                              })}
                            </p>
                          </div>
                        </motion.div>
                      );
                    })}
                    <div ref={messagesEndRef} />
                  </>
                ) : (
                  <div className="text-center text-foreground-muted py-8">
                    <p className="text-lg">👋</p>
                    <p className="mt-2">
                      Start a conversation with {activeConv.partnerUsername}
                    </p>
                  </div>
                )}
              </div>

              <div className="p-4 border-t border-border">
                <div className="flex items-center gap-2">
                  <div className="flex-1 min-w-0">
                    <Input
                      value={messageInput}
                      onChange={(e) => setMessageInput(e.target.value)}
                      onKeyDown={handleKeyDown}
                      placeholder="Type a message..."
                    />
                  </div>
                  <Button
                    onClick={handleSend}
                    disabled={!messageInput.trim() || isSending}
                    isLoading={isSending}
                    className="shrink-0"
                  >
                    Send
                  </Button>
                </div>
              </div>
            </>
          ) : (
            <div className="flex-1 flex items-center justify-center text-foreground-muted">
              <div className="text-center">
                <p className="text-4xl mb-3">💬</p>
                <p className="text-lg font-medium">Select a conversation</p>
                <p className="text-sm mt-1">Or visit someone's profile to start chatting</p>
              </div>
            </div>
          )}
        </div>
      </div>
    </AnimatedPage>
  );
}

function ConversationItem({
  conversation,
  isActive,
  currentUserId,
  onClick,
}: {
  conversation: ConversationResponse;
  isActive: boolean;
  currentUserId: string;
  onClick: () => void;
}) {
  const isUnread = conversation.unreadCount > 0;
  const lastMsg = conversation.lastMessageContent;
  const isOwnLastMsg = conversation.lastMessageSenderId === currentUserId;
  const { data: presence } = usePresence(conversation.partnerId);

  return (
    <button
      className={`w-full flex items-center gap-3 p-3 text-left transition-colors hover:bg-surface-hover ${
        isActive ? 'bg-surface-hover border-l-2 border-primary' : ''
      }`}
      onClick={onClick}
    >
      <div className="relative shrink-0">
        <UserAvatar
          username={conversation.partnerUsername}
          avatarUrl={conversation.partnerAvatarUrl}
          size="sm"
        />
        <span
          className={`absolute bottom-0 right-0 h-2.5 w-2.5 rounded-full border-2 border-surface ${
            presence?.isOnline ? 'bg-green-500' : 'bg-gray-500'
          }`}
        />
      </div>
      <div className="flex-1 min-w-0">
        <div className="flex items-center justify-between">
          <p className={`text-sm truncate ${isUnread ? 'font-bold text-foreground' : 'text-foreground'}`}>
            {conversation.partnerUsername}
          </p>
          {conversation.lastMessageAt && (
            <span className="text-xs text-foreground-muted shrink-0 ml-2">
              {formatTime(conversation.lastMessageAt)}
            </span>
          )}
        </div>
        {lastMsg && (
          <p
            className={`text-xs truncate mt-0.5 ${
              isUnread ? 'text-foreground font-medium' : 'text-foreground-muted'
            }`}
          >
            {isOwnLastMsg ? 'You: ' : ''}
            {lastMsg}
          </p>
        )}
      </div>
      <AnimatePresence>
        {isUnread && (
          <motion.div initial={{ scale: 0 }} animate={{ scale: 1 }} exit={{ scale: 0 }}>
            <Badge variant="primary" className="text-xs">
              {conversation.unreadCount}
            </Badge>
          </motion.div>
        )}
      </AnimatePresence>
    </button>
  );
}

function formatTime(dateStr: string): string {
  const date = new Date(dateStr);
  const now = new Date();
  const diffMs = now.getTime() - date.getTime();
  const diffDays = Math.floor(diffMs / (1000 * 60 * 60 * 24));

  if (diffDays === 0) {
    return date.toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' });
  }
  if (diffDays === 1) return 'Yesterday';
  if (diffDays < 7) return date.toLocaleDateString([], { weekday: 'short' });
  return date.toLocaleDateString([], { month: 'short', day: 'numeric' });
}
