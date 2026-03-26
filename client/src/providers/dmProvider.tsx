import { createContext, useContext, useEffect, useRef, useState, useCallback, type ReactNode } from 'react';
import type { HubConnection } from '@microsoft/signalr';
import { useQueryClient } from '@tanstack/react-query';
import { createDmConnection, startConnection, stopConnection, type ConnectionStatus } from '@/lib/signalr';
import { useAuthStore } from '@/stores/authStore';
import { useToast } from '@/components/ui/toast';
import { queryKeys } from '@/lib/queryKeys';
import type { DirectMessageResponse } from '@/features/dm/types';

interface DmContextValue {
  status: ConnectionStatus;
  sendMessage: (conversationId: string, content: string) => Promise<void>;
  markAsRead: (conversationId: string) => Promise<void>;
  startTyping: (conversationId: string) => Promise<void>;
  stopTyping: (conversationId: string) => Promise<void>;
}

const DmContext = createContext<DmContextValue>({
  status: 'disconnected',
  sendMessage: async () => {},
  markAsRead: async () => {},
  startTyping: async () => {},
  stopTyping: async () => {},
});

export function useDm() {
  return useContext(DmContext);
}

export function DmProvider({ children }: { children: ReactNode }) {
  const connectionRef = useRef<HubConnection | null>(null);
  const qc = useQueryClient();
  const accessToken = useAuthStore((s) => s.accessToken);
  const { addToast } = useToast();
  const [status, setStatus] = useState<ConnectionStatus>('disconnected');

  useEffect(() => {
    if (!accessToken) {
      if (connectionRef.current) {
        stopConnection(connectionRef.current);
        connectionRef.current = null;
      }
      setStatus('disconnected');
      return;
    }

    const conn = createDmConnection(() => useAuthStore.getState().accessToken);
    connectionRef.current = conn;

    conn.on('ReceiveDirectMessage', (msg: DirectMessageResponse) => {
      qc.invalidateQueries({ queryKey: queryKeys.dm.messages(msg.conversationId) });
      qc.invalidateQueries({ queryKey: queryKeys.dm.conversations() });

      const currentUserId = useAuthStore.getState().user?.id;
      if (msg.senderId !== currentUserId) {
        const preview = msg.content.length > 60 ? msg.content.slice(0, 60) + '...' : msg.content;
        addToast({
          title: `💬 ${msg.senderUsername}`,
          message: preview,
          type: 'info',
          duration: 5000,
          onClick: () => {
            window.location.href = `/messages?user=${msg.senderId}`;
          },
        });
      }
    });

    conn.on('MessagesRead', (conversationId: string) => {
      qc.invalidateQueries({ queryKey: queryKeys.dm.messages(conversationId) });
      qc.invalidateQueries({ queryKey: queryKeys.dm.conversations() });
    });

    conn.on('PresenceChanged', (userId: string, isOnline: boolean) => {
      qc.setQueryData(['presence', userId], { isOnline });
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

  const sendMessage = useCallback(async (conversationId: string, content: string) => {
    if (connectionRef.current) {
      await connectionRef.current.invoke('SendDirectMessage', conversationId, content);
    }
  }, []);

  const markAsRead = useCallback(async (conversationId: string) => {
    if (connectionRef.current) {
      await connectionRef.current.invoke('MarkAsRead', conversationId);
    }
  }, []);

  const startTyping = useCallback(async (conversationId: string) => {
    if (connectionRef.current) {
      await connectionRef.current.invoke('StartTyping', conversationId);
    }
  }, []);

  const stopTyping = useCallback(async (conversationId: string) => {
    if (connectionRef.current) {
      await connectionRef.current.invoke('StopTyping', conversationId);
    }
  }, []);

  return (
    <DmContext.Provider value={{ status, sendMessage, markAsRead, startTyping, stopTyping }}>
      {children}
    </DmContext.Provider>
  );
}
