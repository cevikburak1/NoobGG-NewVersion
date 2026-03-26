import { useEffect, useRef, useCallback, useState } from 'react';
import type { HubConnection } from '@microsoft/signalr';
import { useQueryClient } from '@tanstack/react-query';
import { createDmConnection, startConnection, stopConnection } from '@/lib/signalr';
import { useAuthStore } from '@/stores/authStore';
import { queryKeys } from '@/lib/queryKeys';
import type { DirectMessageResponse } from './types';
import type { ConnectionStatus } from '@/lib/signalr';

export function useDmConnection() {
  const connectionRef = useRef<HubConnection | null>(null);
  const qc = useQueryClient();
  const accessToken = useAuthStore((s) => s.accessToken);
  const [status, setStatus] = useState<ConnectionStatus>('disconnected');

  useEffect(() => {
    if (!accessToken) return;

    const conn = createDmConnection(() => useAuthStore.getState().accessToken);
    connectionRef.current = conn;

    conn.on('ReceiveDirectMessage', (msg: DirectMessageResponse) => {
      qc.invalidateQueries({ queryKey: queryKeys.dm.messages(msg.conversationId) });
      qc.invalidateQueries({ queryKey: queryKeys.dm.conversations() });
    });

    conn.on('MessagesRead', (conversationId: string) => {
      qc.invalidateQueries({ queryKey: queryKeys.dm.messages(conversationId) });
      qc.invalidateQueries({ queryKey: queryKeys.dm.conversations() });
    });

    conn.onreconnecting(() => setStatus('reconnecting'));
    conn.onreconnected(() => setStatus('connected'));
    conn.onclose(() => setStatus('disconnected'));

    startConnection(conn)
      .then(() => setStatus('connected'))
      .catch(() => setStatus('disconnected'));

    return () => {
      stopConnection(conn);
      connectionRef.current = null;
    };
  }, [accessToken, qc]);

  const sendMessage = useCallback(
    async (conversationId: string, content: string) => {
      const conn = connectionRef.current;
      if (conn) {
        await conn.invoke('SendDirectMessage', conversationId, content);
      }
    },
    [],
  );

  const markAsRead = useCallback(
    async (conversationId: string) => {
      const conn = connectionRef.current;
      if (conn) {
        await conn.invoke('MarkAsRead', conversationId);
      }
    },
    [],
  );

  const startTyping = useCallback(
    async (conversationId: string) => {
      const conn = connectionRef.current;
      if (conn) {
        await conn.invoke('StartTyping', conversationId);
      }
    },
    [],
  );

  const stopTyping = useCallback(
    async (conversationId: string) => {
      const conn = connectionRef.current;
      if (conn) {
        await conn.invoke('StopTyping', conversationId);
      }
    },
    [],
  );

  return { status, sendMessage, markAsRead, startTyping, stopTyping };
}
