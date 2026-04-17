import { useState, useEffect, useRef } from 'react';
import { useSearchParams } from 'react-router-dom';
import { motion, AnimatePresence } from 'framer-motion';
import { useConversations, useMessages, useCreateConversation, useMarkRead } from '@/features/dm/hooks';
import { useDm } from '@/providers/dmProvider';
import { useAuthStore } from '@/stores/authStore';
import { usePresence } from '@/features/users/usePresence';
import { useToast } from '@/components/ui/toast';
import { Button, Input, Spinner, AnimatedPage } from '@/components/ui';
import { UserAvatar } from '@/components/common/userAvatar';
import { RecentMessagedMini } from '@/components/activity/recentActivitySurfaces';
import type { ConversationResponse } from '@/features/dm/types';

export default function MessagesPage() {
  const [searchParams] = useSearchParams();
  const targetUser = searchParams.get('user');

  const user = useAuthStore((s) => s.user);
  const { data: conversations, isLoading: convsLoading } = useConversations();
  const createConversation = useCreateConversation();
  const markRead = useMarkRead();
  const { sendMessage: sendViaHub, status: dmStatus } = useDm();
  const { addToast } = useToast();

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
      {
        onSuccess: (conv) => setActiveConvId(conv.id),
        onError: (err: unknown) => {
          conversationCreatedRef.current = false;
          const msg = (err as { response?: { data?: { message?: string } } })?.response?.data?.message
            ?? 'Cannot start conversation with this user';
          addToast({ title: 'Error', message: msg, type: 'error' });
        },
      },
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
    } catch (err: unknown) {
      const hubMsg = (err as { message?: string })?.message ?? '';
      const friendlyMsg = hubMsg.includes('friends')
        ? 'Bu kullanıcı yalnızca arkadaşlarından mesaj kabul ediyor.'
        : hubMsg.includes('not accepting')
          ? 'Bu kullanıcı mesaj kabul etmiyor.'
          : 'Mesaj gönderilemedi. Kullanıcı sizi engellemiş veya DM\'leri kapatmış olabilir.';
      addToast({ title: 'Mesaj gönderilemedi', message: friendlyMsg, type: 'error' });
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
      <div className="flex h-full rounded-xl border border-border overflow-hidden bg-surface">
        <div className="w-72 border-r border-border flex flex-col shrink-0">
          <div className="flex items-center justify-between p-4 border-b border-border">
            <h2 className="text-base font-bold text-foreground">Messages</h2>
            <div className="flex items-center gap-1.5">
              <div
                className={`h-2 w-2 rounded-full ${
                  dmStatus === 'connected'
                    ? 'bg-success'
                    : dmStatus === 'reconnecting'
                      ? 'bg-warning animate-pulse'
                      : 'bg-danger'
                }`}
              />
              <span className="text-[11px] text-foreground-subtle capitalize">{dmStatus}</span>
            </div>
          </div>

          <RecentMessagedMini />

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
              <div className="flex flex-col items-center justify-center p-6 text-center">
                <div className="flex h-12 w-12 items-center justify-center rounded-xl bg-surface-hover text-xl">
                  💬
                </div>
                <p className="mt-3 text-sm font-medium text-foreground">No conversations yet</p>
                <p className="mt-1 text-xs text-foreground-muted">
                  Visit a profile and click "Send Message" to start
                </p>
              </div>
            )}
          </div>
        </div>

        <div className="flex-1 flex flex-col min-w-0">
          {activeConv ? (
            <>
              <div className="flex items-center gap-3 border-b border-border px-5 py-3.5">
                <div className="relative">
                  <UserAvatar
                    username={activeConv.partnerUsername}
                    avatarUrl={activeConv.partnerAvatarUrl}
                    size="sm"
                  />
                  <span
                    className={`absolute -bottom-0.5 -right-0.5 h-3 w-3 rounded-full border-2 border-surface ${
                      partnerPresence?.isOnline ? 'bg-success' : 'bg-foreground-subtle'
                    }`}
                  />
                </div>
                <div className="min-w-0 flex-1">
                  <p className="truncate text-sm font-semibold text-foreground">{activeConv.partnerUsername}</p>
                  <p className={`text-xs ${partnerPresence?.isOnline ? 'text-success' : 'text-foreground-subtle'}`}>
                    {partnerPresence?.isOnline ? 'Online' : 'Offline'}
                  </p>
                </div>
              </div>

              <div className="flex-1 overflow-y-auto px-5 py-4 space-y-2.5">
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
                          initial={{ opacity: 0, y: 4 }}
                          animate={{ opacity: 1, y: 0 }}
                          className={`flex ${isOwn ? 'justify-end' : 'justify-start'}`}
                        >
                          <div
                            className={`max-w-[65%] rounded-2xl px-4 py-2.5 ${
                              isOwn
                                ? 'bg-primary text-primary-foreground rounded-br-md'
                                : 'bg-surface-hover text-foreground rounded-bl-md'
                            }`}
                          >
                            <p className="text-sm leading-relaxed whitespace-pre-wrap wrap-break-word">{msg.content}</p>
                            <p
                              className={`text-[11px] mt-1 ${
                                isOwn ? 'text-primary-foreground/60' : 'text-foreground-subtle'
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
                  <div className="flex flex-col items-center justify-center py-12 text-center">
                    <div className="flex h-14 w-14 items-center justify-center rounded-2xl bg-surface-hover text-2xl">
                      👋
                    </div>
                    <p className="mt-3 text-sm font-medium text-foreground">
                      Start a conversation
                    </p>
                    <p className="mt-1 text-xs text-foreground-muted">
                      Say hello to {activeConv.partnerUsername}
                    </p>
                  </div>
                )}
              </div>

              <div className="border-t border-border bg-surface px-4 py-3">
                <div className="flex items-center gap-2.5">
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
                    <svg className="h-4 w-4" fill="none" stroke="currentColor" viewBox="0 0 24 24" strokeWidth={2}>
                      <path strokeLinecap="round" strokeLinejoin="round" d="M6 12L3.269 3.126A59.768 59.768 0 0121.485 12 59.77 59.77 0 013.27 20.876L5.999 12zm0 0h7.5" />
                    </svg>
                  </Button>
                </div>
              </div>
            </>
          ) : (
            <div className="flex-1 flex items-center justify-center">
              <div className="text-center">
                <div className="mx-auto flex h-16 w-16 items-center justify-center rounded-2xl bg-surface-hover/80">
                  <span className="text-3xl">💬</span>
                </div>
                <p className="mt-4 text-base font-semibold text-foreground">Select a conversation</p>
                <p className="mt-1.5 text-sm text-foreground-muted">Or visit someone's profile to start chatting</p>
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
      className={`w-full flex items-center gap-3 px-3 py-3 text-left transition-colors hover:bg-surface-hover ${
        isActive
          ? 'bg-primary/8 border-l-2 border-primary'
          : 'border-l-2 border-transparent'
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
          className={`absolute -bottom-0.5 -right-0.5 h-2.5 w-2.5 rounded-full border-2 border-surface ${
            presence?.isOnline ? 'bg-success' : 'bg-foreground-subtle'
          }`}
        />
      </div>
      <div className="flex-1 min-w-0">
        <div className="flex items-center justify-between">
          <p className={`text-sm truncate ${isUnread ? 'font-bold text-foreground' : 'font-medium text-foreground'}`}>
            {conversation.partnerUsername}
          </p>
          {conversation.lastMessageAt && (
            <span className={`text-[11px] shrink-0 ml-2 ${isUnread ? 'text-primary font-medium' : 'text-foreground-subtle'}`}>
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
            <span className="flex h-5 min-w-[20px] items-center justify-center rounded-full bg-primary px-1.5 text-[11px] font-bold text-primary-foreground">
              {conversation.unreadCount}
            </span>
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
