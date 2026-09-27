import { useEffect, useRef, useState } from 'react';
import { useTranslation } from 'react-i18next';
import { useInfiniteQuery, useQueryClient, type InfiniteData } from '@tanstack/react-query';
import type { HubConnection } from '@microsoft/signalr';
import { api } from '@/lib/api';
import { createGuildChatConnection, startConnection, stopConnection } from '@/lib/signalr';
import { useAuthStore } from '@/stores/authStore';
import { Button } from '@/components/ui';
import { useToast } from '@/components/ui/toast';

interface GuildMessage {
  id: string;
  guildId: string;
  senderId: string;
  senderUsername: string;
  body: string;
  createdAt: string;
}
interface GuildMessagePage { items: GuildMessage[]; nextCursor: string | null }

export function GuildChat({ guildId }: { guildId: string }) {
  const { t } = useTranslation();
  const [body, setBody] = useState('');
  const [sending, setSending] = useState(false);
  const [connected, setConnected] = useState(false);
  const connection = useRef<HubConnection | null>(null);
  const qc = useQueryClient();
  const { addToast } = useToast();
  const messages = useInfiniteQuery({
    queryKey: ['guild-chat', guildId],
    initialPageParam: null as string | null,
    queryFn: async ({ pageParam }) => (await api.get<GuildMessagePage>(`/api/guilds/${guildId}/chat`,
      { params: pageParam ? { beforeId: pageParam } : undefined })).data,
    getNextPageParam: (lastPage) => lastPage.nextCursor ?? undefined,
  });
  const visibleMessages = messages.data?.pages.slice().reverse().flatMap((page) => page.items) ?? [];

  useEffect(() => {
    const conn = createGuildChatConnection(() => useAuthStore.getState().accessToken);
    connection.current = conn;
    conn.on('GuildMessage', (message: GuildMessage) => {
      if (message.guildId !== guildId) return;
      qc.setQueryData<InfiniteData<GuildMessagePage, string | null>>(['guild-chat', guildId], (previous) => {
        if (!previous || previous.pages.some((page) => page.items.some((item) => item.id === message.id))) return previous;
        return { ...previous, pages: previous.pages.map((page, index) => index === 0
          ? { ...page, items: [...page.items, message] } : page) };
      });
    });
    conn.onreconnecting(() => setConnected(false));
    conn.onreconnected(() => setConnected(true));
    conn.onclose(() => setConnected(false));
    void startConnection(conn).then(() => setConnected(true)).catch(() => setConnected(false));
    return () => { connection.current = null; void stopConnection(conn); };
  }, [guildId, qc]);

  const send = async () => {
    if (!body.trim() || !connection.current) return;
    setSending(true);
    try {
      await connection.current.invoke('SendMessage', guildId, body.trim());
      setBody('');
    } catch {
      addToast({ title: t('Message failed'), message: t('Check your connection and membership.'), type: 'error' });
    } finally {
      setSending(false);
    }
  };

  return (
    <section className="border-t border-border pt-5">
      <h2 className="mb-3 text-lg font-semibold text-foreground">{t('Guild chat')}</h2>
      <div className="max-h-80 min-h-36 space-y-2 overflow-y-auto border-y border-border py-3" aria-live="polite">
        {messages.hasNextPage && <Button type="button" size="sm" variant="outline" className="mb-2"
          isLoading={messages.isFetchingNextPage} onClick={() => void messages.fetchNextPage()}>{t('Load older messages')}</Button>}
        {messages.isLoading && <p className="text-sm text-foreground-muted">{t('Loading messages...')}</p>}
        {messages.isError && <p className="text-sm text-danger">{t('Messages could not be loaded.')}</p>}
        {visibleMessages.map((message) => (
          <p key={message.id} className="text-sm text-foreground">
            <span className="mr-2 font-semibold text-primary">{message.senderUsername}</span>{message.body}
          </p>
        ))}
      </div>
      <form className="mt-3 flex gap-2" onSubmit={(event) => { event.preventDefault(); void send(); }}>
        <input aria-label={t('Guild message')} value={body} maxLength={1000} onChange={(event) => setBody(event.target.value)}
          className="min-w-0 flex-1 border border-border bg-surface px-3 py-2 text-sm text-foreground outline-none focus:border-primary" />
        <Button type="submit" disabled={!body.trim() || sending || !connected} isLoading={sending}>{t('Send')}</Button>
      </form>
      {!connected && <p className="mt-2 text-xs text-foreground-muted">{t('Connecting to chat...')}</p>}
    </section>
  );
}
